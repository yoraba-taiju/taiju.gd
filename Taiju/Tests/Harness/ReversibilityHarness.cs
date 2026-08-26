using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using Taiju.Objects;
using Taiju.Objects.Witch;
using Taiju.Scenes.Stages;
using Taiju.UI.HUD;
using Taiju.Util.Reversible;
using Taiju.Util.Reversible.Godot;
using static GdUnit4.Assertions;

namespace Taiju.Tests.Harness;

/**
 * 巻き戻しの往復性ハーネス。
 *
 * 検査する性質はふたつ:
 *
 *   1. **forward で (leap, tick) を最後に処理し終えた瞬間の状態と、
 *      back でそこへ戻ってきた瞬間の状態が一致する。**
 *   2. **leap した瞬間の状態が、分岐元の leap で記録したその tick の状態と一致する。**
 *
 * Dense.Mut は currentTick のスロットに書くので、tick T のスロットには
 * 「T の最後のフレームまで処理した結果」が入る。back で T に戻ると _ProcessBack が
 * そのスロットを Ref で読んで Godot ノードへ書き戻す。よって両者は一致するはずで、
 * 一致しないなら記録漏れ (Dense に載せていない素のフィールドなど) か復元漏れがある。
 *
 * 2 を別に見るのは、leap で通る道が back とは違うため。Dense は leap が変わると
 * Mut では BranchTickOfLeap からの埋め戻し、Ref では AdjustTick に入る。ノード側も
 * _ProcessBack ではなく _ProcessLeap が呼ばれる。leap を跨がないと、この経路は
 * まるごと素通りになる。
 *
 * ## 記録の鍵は (leap, tick)
 *
 * leap した後は同じ tick 番号が別の世界を指す。tick だけを鍵にすると、leap 1 の記録が
 * leap 0 の記録を上書きしてしまう。
 *
 * ## フレームではなく tick を数える
 *
 * ClockNode は leftToTick_ が 0 以下になったフレームでしか Clock.Tick()/Back() を呼ばない。
 * 「n フレーム進めれば n tick 進む」は一般には成り立たないので、ハーネスは 1 フレームずつ
 * 進めては Clock を見て、(leap, tick) ごとにスナップショットを取る。
 * 同じ tick で複数フレーム回った場合、forward では後のフレームで上書きし (= tick の最終状態)、
 * back では最初のフレームだけを比較する (_ProcessBack は同じ記録を読むので何度やっても同じ)。
 *
 * 1 フレームずつ進めるのは ISceneRunner.AwaitIdleFrame() でなければならない。
 * SimulateFrames は指定フレーム数ではなく実時間で待つ実装で、ヘッドレスでは 1 回の呼び出しで
 * 2〜3 tick 進んでしまう。飛ばされた tick は forward の記録が無いので比較対象から漏れ、
 * 「差分 0 件」の空振りに化ける。
 *
 * ## 観測はフレームの中から行う
 *
 * スナップショットを取るのは HarnessObserver の _Process で、await から返ってきた場所ではない。
 * await の再開位置はフレームの物理ステップと _Process の**あいだ**にあり、そこでは
 * ReversibleRigidBody3D 派生が 1 ステップ先の位置に居る。理由は HarnessObserver のコメント。
 *
 * ## 空振りで緑にならないための検査
 *
 * このハーネスは何も起きなくても「差分 0 件」で緑になれてしまう。実際、魔素が足りないと
 * Player.ProcessBackButton は StartBack に入らず ClockOperation.Stop に落ちる
 * (BackAsync が毎回魔素を満タンにするのはそのため)。AssertRoundTrip はそれを潰すために
 *   - back で実際に tick が期待数だけ減ったか
 *   - 突き合わせた区間で監視対象がそもそも動いたか
 * も併せて検査する。
 *
 * ## 見えないもの
 *
 * 観測しているのは Node3D.Transform と IReversibleNode.IsAlive だけ。
 * Dense に載せていない素のフィールドの食い違いは、それが位置か生死に出るまで検出できない
 * (実例: EnemyBase.displayed_ → https://code.ledyba.org/yoraba-taiju/taiju.gd/issues/35)。
 */
internal sealed class ReversibilityHarness : IDisposable {
  /** 骨格 (StageSkeleton.tscn) を継承したハーネス用ステージ。 */
  private const string ScenePath = "res://Tests/Harness/HarnessStage.tscn";

  /**
   * forward で進められる tick 数の上限。
   *
   * Clock.Back() で戻れるのは 255 tick まで、ClockNode.ProcessDestroy がノードを本当に
   * QueueFree するのは DestroyedAt + 256 < CurrentTick から。ここを超えると
   * 「戻れないから違う」「解放済みだから居ない」が本物の差分に混ざって読めなくなるので、
   * 余裕をとったところで例外にする。
   */
  public const uint MaxForwardTicks = 200;

  /** 進捗が無いまま回り続けたときに諦めるフレーム数。 */
  private const uint StallLimitFrames = 600;

  /** 失敗メッセージが読める長さに収まるよう、差分はここまでしか溜めない。 */
  private const int MaxReportedDiffs = 20;

  /** 記録の鍵。leap が違えば同じ tick でも別の世界。 */
  private readonly record struct Key(uint Leap, uint Tick) {
    public override string ToString() => $"(leap {Leap}, tick {Tick})";
  }

  private readonly record struct NodeState(string Path, Transform3D Transform, bool IsAlive);

  private readonly record struct Diff(Key Expected, Key Actual, string Path, string What, string Left, string Right) {
    public override string ToString() =>
      Expected == Actual
        ? $"  {Actual} {Path} の {What}: forward では {Left} / back では {Right}"
        : $"  {Actual} {Path} の {What}: {Expected} では {Left} / leap 直後は {Right}";
  }

  /** 観測点が何をするフェーズか。 */
  private enum Phase {
    Idle,
    Forward,
    Back,
    Leap,
  }

  private readonly ISceneRunner runner_;
  private readonly HarnessObserver observer_;
  private readonly Clock clock_;
  private readonly Sora sora_;
  private readonly Node watchRoot_;
  private readonly Func<Node, bool> select_;
  private readonly Dictionary<Key, Dictionary<ulong, NodeState>> forward_ = new();
  private readonly HashSet<Key> comparedKeys_ = [];
  private readonly List<Diff> diffs_ = [];
  private int droppedDiffs_;
  private Phase phase_ = Phase.Idle;
  private uint leapBranchedFrom_;

  public Node3D Scene { get; }
  public uint CurrentTick => clock_.CurrentTick;
  public uint CurrentLeap => clock_.CurrentLeap;

  private ReversibilityHarness(
    ISceneRunner runner, Node3D scene, HarnessObserver observer, Node watchRoot, Func<Node, bool> select) {
    runner_ = runner;
    Scene = scene;
    watchRoot_ = watchRoot;
    select_ = select;
    observer_ = observer;
    clock_ = scene.GetNode<ClockNode>("Clock").Clock;
    sora_ = scene.GetNode<Sora>("Field/Witch/Sora");
    observer.OnProcessed = OnFrameProcessed;
  }

  /**
   * シーンをツリーから外す。
   *
   * ISceneRunner は破棄しないとシーンが /root にぶら下がったまま残り、次のテストが
   * 読み込んだ骨格と名前がぶつかる。全ノードが "/root/Root/..." の絶対パスで互いを掴んでいるので、
   * 残骸があると新しいシーンのノードが古い Clock を掴んで動かなくなる。
   * テストからは using で受けること。
   */
  public void Dispose() {
    observer_.OnProcessed = null;
    runner_.Dispose();
  }

  private Key Here => new(clock_.CurrentLeap, clock_.CurrentTick);

  private void OnFrameProcessed() {
    switch (phase_) {
      case Phase.Forward:
        forward_[Here] = Capture();
        break;
      case Phase.Back:
        Compare(Here, Here);
        break;
      case Phase.Leap:
        // leap が実際に進んだフレームでだけ、分岐元の記録と突き合わせる。
        if (clock_.CurrentLeap != leapBranchedFrom_) {
          Compare(new Key(leapBranchedFrom_, clock_.CurrentTick), Here);
          phase_ = Phase.Idle;
        }
        break;
      case Phase.Idle:
        break;
    }
  }

  /**
   * ハーネス用ステージを組み立てて起動する。
   *
   * populate は「シーンがまだツリーに入っていない」段階で呼ばれる。Godot はノード生成時に
   * 引数を渡せないので、_Ready より前に仕込みたいもの (検体の配置、Stager への注入) は
   * ここでやる。PackedScene.Instantiate() の時点では _Ready はまだ走らない。
   *
   * watchPath は監視根。既定の "Field/Enemy" は敵だけを見る。
   * select は監視対象の絞り込み。既定は IReversibleNode な Node3D だけで、
   * アニメーション駆動の子ノード (Drone1 の AnimationTree 配下など) は見ない。
   * あれは巻き戻しの記録ではなく seek で復元されるので、本物のバグでない差分が出る。
   */
  public static async Task<ReversibilityHarness> BootAsync(
    Action<Node3D> populate = null,
    string watchPath = "Field/Enemy",
    Func<Node, bool> select = null) {
    var scene = GD.Load<PackedScene>(ScenePath).Instantiate<Node3D>();
    var stager = scene.GetNode<HarnessStager>("Stager");
    // Stager._ProcessForward が Stage.Events を、_ExitTree が ResourceManager を
    // 無条件に触るので、_Ready より前に埋めておく。詳細は HarnessStager のコメント。
    stager.Stage = new Scenes.Model.Stage { Events = [] };
    stager.ResourceManager = NewResourceManager();
    populate?.Invoke(scene);
    // ルートの最後の子にすることで、どのノードよりも後に _Process が回る。
    var observer = new HarnessObserver { Name = "HarnessObserver" };
    scene.AddChild(observer);

    var runner = ISceneRunner.Load(scene, true);
    // _Ready を走らせる。
    await runner.AwaitIdleFrame();

    var watchRoot = scene.GetNode<Node>(watchPath)
                    ?? throw new InvalidOperationException($"監視根 \"{watchPath}\" が無い。");
    return new ReversibilityHarness(runner, scene, observer, watchRoot, select ?? IsReversible);
  }

  /**
   * 本番の StageLoader と同じものを積んだ ResourceManager。
   *
   * 空でも起動はするが、leap すると Sora が SoraClone を生もうとして落ちる。
   * ResourceManager.Instantiate はキャッシュに無いと Load して Add した上でもう一度 Add するので、
   * 取りこぼしは ArgumentException になる (https://code.ledyba.org/yoraba-taiju/taiju.gd/issues/33)。
   */
  private static ResourceManager NewResourceManager() {
    var manager = new ResourceManager();
    foreach (var path in StageLoader.CommonPreloadScenes) {
      manager.Add(path, ResourceLoader.Load(path));
    }
    return manager;
  }

  private static bool IsReversible(Node node) => node is Node3D and IReversibleNode;

  /**
   * ticks 分だけ時間を進め、tick ごとの最終状態を記録する。
   *
   * 記録が始まるのは最初の 1 フレームを回した後なので、開始時点の tick は記録されない。
   * BackAsync に ticks と同じ数を渡すとその 1 個が足りなくなる。forward は back より
   * 1 tick 以上多く進めること。
   */
  public async Task ForwardAsync(uint ticks) {
    if (ticks > MaxForwardTicks) {
      throw new ArgumentOutOfRangeException(nameof(ticks), ticks,
        $"forward は {MaxForwardTicks} tick までにすること。" +
        "これを超えると巻き戻し可能範囲 (255 tick) や墓場の解放境界 (256 tick) に掛かり、" +
        "本物の差分と区別できない差分が出る。");
    }

    var target = clock_.CurrentTick + ticks;
    phase_ = Phase.Forward;
    try {
      var stalled = 0u;
      while (clock_.CurrentTick < target) {
        var before = clock_.CurrentTick;
        await runner_.AwaitIdleFrame();
        if (clock_.CurrentTick < before) {
          // 自機が被弾すると Player は ClockState.OnDamage に入り、入力に関係なく
          // 巻き戻しを始める。放っておくと tick 0 まで戻り、そこで Clock.Back() が
          // 失敗して二度と進まなくなる (魔素が減らないので OnDamage から抜けられない)。
          // https://code.ledyba.org/yoraba-taiju/taiju.gd/issues/34
          throw new InvalidOperationException(
            $"forward 中に時間が巻き戻った (tick {before} -> {clock_.CurrentTick})。" +
            "自機が被弾して ClockState.OnDamage に入っている。" +
            "検体が自機に届かない配置と tick 数にすること。");
        }
        stalled = clock_.CurrentTick == before ? stalled + 1 : 0;
        if (stalled > StallLimitFrames) {
          throw new InvalidOperationException(
            $"{StallLimitFrames} フレーム回しても tick {clock_.CurrentTick} から進まない。");
        }
      }
    } finally {
      phase_ = Phase.Idle;
    }
  }

  /**
   * ticks 分だけ巻き戻し、戻ってきた各 tick で forward の記録と突き合わせる。
   *
   * time_back は back フェーズの間ずっと押しっぱなしにする。途中で離すと
   * Player が IsActionJustReleased を拾って ClockOperation.Leap に入ってしまう。
   * 意図して leap したいときは、これを呼んだ直後に LeapAsync を呼ぶ。
   */
  public async Task BackAsync(uint ticks) {
    var target = clock_.CurrentTick - ticks;
    var oldest = OldestRecordedTickOfCurrentLeap();
    if (oldest == null || target < oldest) {
      throw new ArgumentOutOfRangeException(nameof(ticks), ticks,
        $"tick {target} まで戻ろうとしているが、leap {clock_.CurrentLeap} の記録は " +
        $"tick {(oldest?.ToString() ?? "無し")} からしかない。forward を back より 1 tick 以上多く進めること。");
    }

    // 魔素はハーネスの検査対象ではない。1 tick 戻るごとに減り、尽きると Player は
    // ClockOperation.Stop に落ちる。そうなると「巻き戻せなかった」だけの失敗になって
    // 往復性について何も言えないので、back に入る前に満タンにしておく。
    // 魔素は巻き戻しても戻らない (Player.state_ は素の struct) ので、足すのは毎回必要。
    sora_.AbsorbMagicElement(SpellGauge.MaxItems);

    runner_.SimulateActionPress("time_back");
    phase_ = Phase.Back;
    try {
      var stalled = 0u;
      while (clock_.CurrentTick > target) {
        var before = clock_.CurrentTick;
        await runner_.AwaitIdleFrame();
        stalled = clock_.CurrentTick == before ? stalled + 1 : 0;
        if (stalled > StallLimitFrames) {
          // 魔素切れで ClockOperation.Stop に落ちたときもここに来る。
          throw new InvalidOperationException(
            $"{StallLimitFrames} フレーム押しても tick {clock_.CurrentTick} から戻らない " +
            $"(目標 {target})。魔素が足りていない可能性がある。");
        }
      }
    } finally {
      phase_ = Phase.Idle;
      runner_.SimulateActionRelease("time_back");
    }
  }

  /**
   * 巻き戻しをやめ、その地点から新しい leap を始める。**BackAsync の直後にだけ呼べる。**
   *
   * leap は「time_back を離した」ことで起きる。BackAsync は最後に離しているが、
   * そのあとフレームを回していないので Input.IsActionJustReleased はまだ立っていない。
   * ここでフレームを回すと Player がそれを拾って ClockOperation.Leap になり、
   * ClockNode が Clock.Leap() を呼び、各ノードに _ProcessLeap が飛ぶ。
   *
   * leap は tick を進めない。分岐した瞬間の状態は分岐元の記録と一致しなければならず、
   * それをこの中で突き合わせる。
   */
  public async Task LeapAsync() {
    leapBranchedFrom_ = clock_.CurrentLeap;
    if (!forward_.ContainsKey(new Key(leapBranchedFrom_, clock_.CurrentTick))) {
      throw new InvalidOperationException(
        $"leap しようとしている {Here} の記録が無い。ForwardAsync -> BackAsync の順で呼ぶこと。");
    }

    phase_ = Phase.Leap;
    try {
      var stalled = 0u;
      while (clock_.CurrentLeap == leapBranchedFrom_) {
        await runner_.AwaitIdleFrame();
        if (++stalled > StallLimitFrames) {
          throw new InvalidOperationException(
            $"{StallLimitFrames} フレーム回しても leap {leapBranchedFrom_} から分岐しない。" +
            "LeapAsync は BackAsync の直後に呼ぶこと (time_back を離した瞬間が leap の合図)。");
        }
      }
    } finally {
      phase_ = Phase.Idle;
    }
  }

  /** 突き合わせた (leap, tick) の数。 */
  public int ComparedCount => comparedKeys_.Count;

  /**
   * 突き合わせた区間で監視対象が動いたか。
   * false なら静止物を比べていただけで、往復性を何も検査していない。
   */
  public bool ObservedMotion {
    get {
      foreach (var group in comparedKeys_.GroupBy(key => key.Leap)) {
        var ticks = group.Select(key => key.Tick).ToArray();
        if (ticks.Length < 2) {
          continue;
        }
        if (Differs(forward_[new Key(group.Key, ticks.Min())], forward_[new Key(group.Key, ticks.Max())])) {
          return true;
        }
      }
      return false;
    }
  }

  private static bool Differs(
    Dictionary<ulong, NodeState> left, Dictionary<ulong, NodeState> right) {
    if (left.Count != right.Count) {
      return true;
    }
    foreach (var (id, a) in left) {
      if (!right.TryGetValue(id, out var b)) {
        return true;
      }
      if (a.Transform != b.Transform || a.IsAlive != b.IsAlive) {
        return true;
      }
    }
    return false;
  }

  /** 差分の一覧。空文字列なら往復性は保たれている。 */
  public string DiffReport {
    get {
      if (diffs_.Count == 0) {
        return "";
      }
      var sb = new StringBuilder();
      sb.Append(CultureInfo.InvariantCulture, $"往復性が破れている ({diffs_.Count + droppedDiffs_} 件):\n");
      foreach (var diff in diffs_) {
        sb.Append(diff).Append('\n');
      }
      if (droppedDiffs_ > 0) {
        sb.Append(CultureInfo.InvariantCulture, $"  ... 他 {droppedDiffs_} 件\n");
      }
      return sb.ToString();
    }
  }

  /**
   * 往復性と、ハーネス自身が空振りしていないことを検査する。
   * expectedComparisons には突き合わせが起きるはずの回数 (BackAsync に渡した tick 数の合計 +
   * LeapAsync を呼んだ回数) を渡す。
   */
  public void AssertRoundTrip(uint expectedComparisons) {
    AssertThat(comparedKeys_.Count)
      .OverrideFailureMessage(
        $"突き合わせた回数が {comparedKeys_.Count} しかない (期待 {expectedComparisons} 以上)。" +
        "巻き戻しが起きていないか、forward の記録が残っていない。")
      .IsGreaterEqual((int)expectedComparisons);
    AssertThat(ObservedMotion)
      .OverrideFailureMessage(
        "突き合わせた区間で監視対象がまったく動いていない。静止物を比べているだけで、往復性を検査できていない。")
      .IsTrue();
    var report = DiffReport;
    AssertThat(report)
      // OverrideFailureMessage は空文字列を受け付けないので、通る側にも文字列を用意しておく。
      .OverrideFailureMessage(report.Length > 0 ? report : "(差分なし)")
      .IsEqual("");
  }

  private uint? OldestRecordedTickOfCurrentLeap() {
    var leap = clock_.CurrentLeap;
    uint? oldest = null;
    foreach (var key in forward_.Keys) {
      if (key.Leap == leap && (oldest == null || key.Tick < oldest)) {
        oldest = key.Tick;
      }
    }
    return oldest;
  }

  private void Compare(Key expected, Key actual) {
    if (!forward_.TryGetValue(expected, out var want)) {
      return;
    }
    // 同じ tick で複数フレーム回ることがあるが、_ProcessBack は毎回同じ記録を読むので 1 回でよい。
    if (!comparedKeys_.Add(actual)) {
      return;
    }

    var got = Capture();
    foreach (var (id, a) in want) {
      if (!got.TryGetValue(id, out var b)) {
        AddDiff(expected, actual, a.Path, "存在", "居る", "居ない");
        continue;
      }
      if (a.Transform != b.Transform) {
        AddDiff(expected, actual, a.Path, "Transform", Format(a.Transform), Format(b.Transform));
      }
      if (a.IsAlive != b.IsAlive) {
        AddDiff(expected, actual, a.Path, "IsAlive", a.IsAlive.ToString(), b.IsAlive.ToString());
      }
    }
    foreach (var (id, b) in got) {
      if (!want.ContainsKey(id)) {
        AddDiff(expected, actual, b.Path, "存在", "居ない", "居る");
      }
    }
  }

  private void AddDiff(Key expected, Key actual, string path, string what, string left, string right) {
    if (diffs_.Count >= MaxReportedDiffs) {
      droppedDiffs_++;
      return;
    }
    diffs_.Add(new Diff(expected, actual, path, what, left, right));
  }

  private Dictionary<ulong, NodeState> Capture() {
    var into = new Dictionary<ulong, NodeState>();
    Walk(watchRoot_, into);
    return into;
  }

  private void Walk(Node node, Dictionary<ulong, NodeState> into) {
    foreach (var child in node.GetChildren()) {
      if (select_(child) && child is Node3D node3d) {
        into[child.GetInstanceId()] = new NodeState(
          child.GetPath().ToString(),
          node3d.Transform,
          child is not IReversibleNode reversible || reversible.IsAlive);
      }
      Walk(child, into);
    }
  }

  private static string Format(in Transform3D transform) {
    var o = transform.Origin;
    return string.Create(CultureInfo.InvariantCulture, $"({o.X:R}, {o.Y:R}, {o.Z:R})");
  }
}
