using System;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace Taiju.Tests.Harness;

/**
 * ハーネス自身の検査。合成検体 (Probes.cs) を使って、
 * ReversibilityHarness が緑にすべきものを緑に、赤にすべきものを赤にすることを確かめる。
 *
 * これが通っていれば、本物の敵で赤が出たときに「ハーネスのバグ」を疑わなくて済む。
 */
[TestSuite]
public class HarnessSelfTest {
  private const uint ForwardTicks = 120;
  private const uint BackTicks = 60;

  // leap の先で回す分。forward は back より 1 tick 多く要る。
  private const uint LeapForwardTicks = 40;
  private const uint LeapBackTicks = 30;

  private static Task<ReversibilityHarness> BootWithAsync<T>(Action<T> configure = null)
    where T : Node3D, new() =>
    ReversibilityHarness.BootAsync(scene => {
      var probe = new T { Name = typeof(T).Name };
      configure?.Invoke(probe);
      scene.GetNode<Node3D>("Field/Enemy/DefaultRush").AddChild(probe);
    });

  [TestCase]
  [RequireGodotRuntime]
  public async Task FunctionProbeRoundTrips() {
    using var harness = await BootWithAsync<FunctionProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    harness.AssertRoundTrip();
  }

  [TestCase]
  [RequireGodotRuntime]
  public async Task RecordingProbeRoundTrips() {
    using var harness = await BootWithAsync<RecordingProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    harness.AssertRoundTrip();
  }

  /**
   * leap を跨いだ往復性。RecordingProbe は _ProcessLeap を実装していないので、
   * leap 後の最初の Mut が Dense の BranchTickOfLeap からの埋め戻し経路に入る。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task RecordingProbeRoundTripsAcrossLeap() {
    using var harness = await BootWithAsync<RecordingProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    await harness.LeapAsync();

    AssertThat(harness.CurrentLeap).IsEqual(1u);

    await harness.ForwardAsync(LeapForwardTicks);
    await harness.BackAsync(LeapBackTicks);
    harness.AssertRoundTrip();
  }

  [TestCase]
  [RequireGodotRuntime]
  public async Task FunctionProbeRoundTripsAcrossLeap() {
    using var harness = await BootWithAsync<FunctionProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    await harness.LeapAsync();
    await harness.ForwardAsync(LeapForwardTicks);
    await harness.BackAsync(LeapBackTicks);
    harness.AssertRoundTrip();
  }

  /**
   * 分岐点より前へ戻る。プレイヤーが実際にやる「戻す → 離す → もっと深く戻す」の形。
   *
   * ここは分岐元の leap が記録した領域なので、期待値もそちらから取る必要がある。
   * leap 完全一致で期待値を引いていると「記録が無い」と言って拒否してしまう。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task BackingBelowTheBranchPointRoundTrips() {
    using var harness = await BootWithAsync<RecordingProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    await harness.LeapAsync();
    var branchTick = harness.CurrentTick;

    // 分岐点から 10 進んで、そこから 40 戻る (= 分岐点より 30 tick 手前へ)。
    await harness.ForwardAsync(10);
    await harness.BackAsync(40);

    AssertThat(harness.CurrentLeap).IsEqual(1u);
    AssertThat(harness.CurrentTick).IsLess(branchTick);
    harness.AssertRoundTrip();
  }

  /**
   * 壊れた検体をちゃんと赤にできること。
   * これが通らないなら、他のテストの緑は「何も見ていない」ことの証明でしかない。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task LeakyProbeIsReportedAsBroken() {
    using var harness = await BootWithAsync<LeakyProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);

    AssertThat(harness.ObservedMotion).IsTrue();
    AssertThat(harness.DiffReport).IsNotEmpty();
  }

  /**
   * leap の瞬間の壊れをちゃんと赤にできること。
   *
   * これが無いと「leap の瞬間に 1 回突き合わせた」という回数だけが増えて、
   * 中身は恒真という状態に気づけない。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task LeapLeakyProbeIsReportedAsBroken() {
    using var harness = await BootWithAsync<LeapLeakyProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    await harness.LeapAsync();

    // leap の瞬間の突き合わせは起きていて、そこで差分が出ている。
    AssertThat(harness.LeapComparisons).IsEqual(1);
    AssertThat(harness.DiffReport).IsNotEmpty();
  }

  /**
   * back だけなら同じ検体が緑になること。
   * 直前のテストの赤が「leap の経路」由来であることの裏取り。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task LeapLeakyProbeIsGreenWithoutLeaping() {
    using var harness = await BootWithAsync<LeapLeakyProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    harness.AssertRoundTrip();
  }

  /**
   * Destroy / Rescue を窓の中で通す。
   *
   * **これは現状を固定するテストで、緑であることは「正しい」を意味しない。**
   * `ClockNode.ProcessRescue` の復活条件が `DestroyedAt >= CurrentTick` なので、
   * 破壊した tick ちょうどに戻るとそこだけノードが生き返る
   * ([#38](https://code.ledyba.org/yoraba-taiju/taiju.gd/issues/38))。
   * ずれは境界の 1 tick だけで、他の tick は一致する ── その形まで含めて固定している。
   *
   * #38 を直したらこのテストは赤になる。そのときは差分 0 件を期待する形に書き換えること。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task DyingProbeShowsTheKnownRescueOffByOne() {
    const uint dieAtTick = 60;
    using var harness = await BootWithAsync<DyingProbe>(probe => probe.DieAtTick = dieAtTick);
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(100);

    var report = harness.DiffReport;
    AssertThat(harness.DiffCount).OverrideFailureMessage(
      $"差分は破壊 tick の 1 件だけのはず。実際の報告:\n{(report.Length > 0 ? report : "(差分なし)")}")
      .IsEqual(1);
    AssertThat(report.Contains($"(leap 0, tick {dieAtTick})")).OverrideFailureMessage(
      $"差分が破壊 tick ({dieAtTick}) 以外に出ている:\n{report}").IsTrue();
    AssertThat(report.Contains("IsAlive")).OverrideFailureMessage(
      $"差分が IsAlive 以外に出ている:\n{report}").IsTrue();
  }

  /**
   * 静止物しか居ないときは「往復性を検査できていない」と判定すること。
   * 差分は 0 件 (往復性自体は自明に成り立つ) だが、それを緑と呼んではいけない。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task StationaryProbeIsNotMistakenForSuccess() {
    using var harness = await BootWithAsync<StationaryProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);

    AssertThat(harness.DiffReport).IsEqual("");
    AssertThat(harness.ObservedMotion).IsFalse();
  }

  /**
   * 静止物で leap まで通しても、例外を投げずに「検査できていない」と答えること。
   *
   * leap の瞬間の鍵 (新しい leap, 分岐 tick) には記録が無い (leap は tick を進めないので、
   * その leap の最初の記録は分岐 tick + 1 から)。ここを記録の辞書から素引きしていると
   * KeyNotFoundException になる。動いている検体だと leap 0 側の評価で先に真が返って
   * 隠れるので、静止物でしか出ない。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task StationaryProbeAcrossLeapDoesNotThrow() {
    using var harness = await BootWithAsync<StationaryProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    await harness.LeapAsync();
    await harness.ForwardAsync(LeapForwardTicks);
    await harness.BackAsync(LeapBackTicks);

    AssertThat(harness.ObservedMotion).IsFalse();
    AssertThat(harness.DiffReport).IsEqual("");
  }

  /**
   * 巻き戻しが実際に起きていること。空振りしていないことの土台。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task BackPhaseActuallyRewinds() {
    using var harness = await BootWithAsync<FunctionProbe>();
    await harness.ForwardAsync(ForwardTicks);
    var peak = harness.CurrentTick;

    await harness.BackAsync(BackTicks);

    AssertThat(harness.CurrentTick).IsEqual(peak - BackTicks);
    AssertThat(harness.BackComparisons).IsEqual((int)BackTicks);
  }

  /**
   * leap は tick を進めない。分岐した瞬間は分岐元と同じ時刻に居る。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task LeapBranchesWithoutMovingTheClock() {
    using var harness = await BootWithAsync<RecordingProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    var branchTick = harness.CurrentTick;

    await harness.LeapAsync();

    AssertThat(harness.CurrentLeap).IsEqual(1u);
    AssertThat(harness.CurrentTick).IsEqual(branchTick);
    AssertThat(harness.DiffReport).IsEqual("");
  }

  /**
   * ハーネスが扱える範囲 (累積 tick) を超える forward は、差分の解釈が不能になるので拒否すること。
   * 1 回の引数ではなく絶対 tick で見ていること ── 分割して呼んでも越えられないこと ── も含む。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task ForwardBeyondTheWindowIsRefused() {
    using var harness = await BootWithAsync<FunctionProbe>();
    (await AssertThrown(harness.ForwardAsync(ReversibilityHarness.MaxForwardTicks + 1)))
      .IsInstanceOf<ArgumentOutOfRangeException>();

    await harness.ForwardAsync(ReversibilityHarness.MaxForwardTicks);
    (await AssertThrown(harness.ForwardAsync(1)))
      .IsInstanceOf<ArgumentOutOfRangeException>();
  }

  /**
   * いまの tick より多く戻そうとしたら拒否すること。
   * uint なので、素で引くと巨大な目標に化けて「1 度も巻き戻さず正常終了」になる。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task BackingFurtherThanTheClockIsRefused() {
    using var harness = await BootWithAsync<FunctionProbe>();
    await harness.ForwardAsync(20);
    (await AssertThrown(harness.BackAsync(100)))
      .IsInstanceOf<ArgumentOutOfRangeException>();
  }
}
