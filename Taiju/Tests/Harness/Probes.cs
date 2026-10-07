using Godot;
using Taiju.Util.Reversible.Godot;
using Taiju.Util.Reversible.Value;

namespace Taiju.Tests.Harness;

/**
 * ハーネス自身を検査するための合成検体。
 *
 * 本物の敵を最初の検体にすると、赤が出たときに「ハーネスのバグ」と「敵のバグ」を
 * 切り分けられない。ここに置いてあるのは往復性が自明に成り立つ / 自明に壊れているノードで、
 * ハーネスがそれぞれ緑・赤を出すことを HarnessSelfTest が確かめる。
 */

/**
 * f(t) 方式の検体。位置を integrateTime だけの関数にしてあるので、
 * forward と back で同じ式を評価すれば必ず一致する。
 * integrateTime は ReversibleCompanion が Dense<double> で記録しているので巻き戻る。
 */
public partial class FunctionProbe : ReversibleNode3D {
  private const float Speed = 3.0f;

  private static Vector3 At(double integrateTime) =>
    new((float)(integrateTime * Speed), (float)Mathf.Sin(integrateTime), 0.0f);

  public override bool _ProcessForward(double integrateTime, double dt) {
    Position = At(integrateTime);
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    Position = At(integrateTime);
    return true;
  }
}

/**
 * 履歴記録方式の検体。毎フレーム Dense に積んで、back では記録から書き戻す。
 * Mut は currentTick のスロットに書くので、tick T のスロットには T の最終フレームの値が入る。
 *
 * _ProcessLeap を実装していないので、leap のフレームでは Record.Ref を読まない。
 * その結果 lastTouchedLeap_ が分岐元のまま残り、leap 後の最初の Mut が
 * Dense の BranchTickOfLeap からの埋め戻し経路に入る。本物の敵はいずれも
 * _ProcessLeap で復元する (= leap のフレームで Ref を読む) ので、この経路を通るのは
 * いまこの検体だけ。
 */
public partial class RecordingProbe : ReversibleNode3D {
  private static readonly Vector3 Velocity = new(1.5f, -0.75f, 0.25f);
  private Dense<Vector3> record_;

  public override void _Ready() {
    base._Ready();
    record_ = new Dense<Vector3>(Clock, Vector3.Zero);
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var position = ref record_.Mut;
    position += Velocity * (float)dt;
    Position = position;
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    Position = record_.Ref;
    return true;
  }
}

/**
 * わざと往復性を壊した検体。位置を Dense ではなく素のフィールドに持ち、back で何もしない。
 * ハーネスがこれを赤にできなければ、緑には意味が無い。
 */
public partial class LeakyProbe : ReversibleNode3D {
  private static readonly Vector3 Velocity = new(2.0f, 0.0f, 0.0f);
  private Vector3 position_;

  public override bool _ProcessForward(double integrateTime, double dt) {
    position_ += Velocity * (float)dt;
    Position = position_;
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    return true;
  }
}

/**
 * back は正しく復元するが、leap のフレームだけわざと嘘を書く検体。
 *
 * これが無いと、leap の瞬間の突き合わせは恒真になる。FunctionProbe と RecordingProbe は
 * どちらも _ProcessLeap を実装しておらず、leap のフレームではノードに何も書かれないので、
 * 観測される状態は直前の back フレームの結果そのまま ── つまり既に突き合わせ済みのものになる。
 * ハーネスが「leap の壊れ」を赤にできることは、この検体でしか示せない。
 */
public partial class LeapLeakyProbe : ReversibleNode3D {
  private static readonly Vector3 Velocity = new(1.0f, 0.5f, 0.0f);

  /** leap のフレームで足す嘘。位置の差として観測できる大きさにする。 */
  private static readonly Vector3 Lie = new(5.0f, 0.0f, 0.0f);

  private Dense<Vector3> record_;

  public override void _Ready() {
    base._Ready();
    record_ = new Dense<Vector3>(Clock, Vector3.Zero);
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var position = ref record_.Mut;
    position += Velocity * (float)dt;
    Position = position;
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    Position = record_.Ref;
    return true;
  }

  public override bool _ProcessLeap(double integrateTime) {
    Position = record_.Ref + Lie;
    return true;
  }
}

/**
 * 窓の中で自分を Destroy する検体。
 *
 * Destroy / Rescue は巻き戻し機構でいちばん壊れやすいところなのに、
 * 検体が誰も窓の中で死ななければこの分岐はまるごと未実行になる。
 */
public partial class DyingProbe : ReversibleNode3D {
  private static readonly Vector3 Velocity = new(1.0f, 0.0f, 0.0f);

  /** ここまで来たら死ぬ。 */
  public uint DieAtTick { get; set; } = 60;

  private Dense<Vector3> record_;

  public override void _Ready() {
    base._Ready();
    record_ = new Dense<Vector3>(Clock, Vector3.Zero);
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var position = ref record_.Mut;
    position += Velocity * (float)dt;
    Position = position;
    if (IsAlive && Clock.CurrentTick >= DieAtTick) {
      Destroy();
    }
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    Position = record_.Ref;
    return true;
  }

  public override bool _ProcessLeap(double integrateTime) {
    Position = record_.Ref;
    return true;
  }
}

/**
 * 物理シグナル (body_entered) の中から自分を Destroy する検体。弾で倒される経路の模型。
 *
 * 本物の撃破は SoraBullet.OnBodyEntered → EnemyBase.Hit → Destroy() で、物理ステップの中で
 * 走る。そこではまだ ClockNode が tick していないので、DestroyedAt には 1 つ前の tick が入る。
 * DyingProbe (_Process の中で Destroy する) とは DestroyedAt の意味が違うので、別の検体にしてある。
 *
 * 壊れる tick はフレームの刻みで前後する (実測で 42〜44) ので、テストは DestroyedAtTick を見る。
 */
public partial class PhysicsDyingProbe : ReversibleRigidBody3D {
  /** 他の物理ノードとぶつからない専用レイヤ。 */
  private const uint Layer = 1u << 19;

  /** Destroy() したときの Clock.CurrentTick。まだ壊れていなければ uint.MaxValue。 */
  public uint DestroyedAtTick { get; private set; } = uint.MaxValue;

  private Dense<Vector3> record_;

  /**
   * 検体と、その進路に置いた壁を parent の下に並べる。
   * 原点から +X へ 3/s で進み、x = 3 の壁に 40 tick ほどで当たる。
   */
  public static PhysicsDyingProbe PlaceWithWall(Node3D parent) {
    var probe = new PhysicsDyingProbe {
      Name = nameof(PhysicsDyingProbe),
      CollisionLayer = Layer,
      CollisionMask = Layer,
    };
    probe.AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = 0.5f } });
    var wall = new StaticBody3D {
      Name = "Wall",
      Position = new Vector3(3.0f, 0.0f, 0.0f),
      CollisionLayer = Layer,
      CollisionMask = Layer,
    };
    wall.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = Vector3.One } });
    parent.AddChild(probe);
    parent.AddChild(wall);
    return probe;
  }

  public override void _Ready() {
    base._Ready();
    GravityScale = 0.0f;
    ContactMonitor = true;
    MaxContactsReported = 4;
    record_ = new Dense<Vector3>(Clock, Position);
    BodyEntered += OnBodyEntered;
  }

  private void OnBodyEntered(Node node) {
    if (!IsAlive) {
      return;
    }
    DestroyedAtTick = Clock.CurrentTick;
    Destroy();
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    state.LinearVelocity = new Vector3(3.0f, 0.0f, 0.0f);
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    record_.Mut = Position;
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    Position = record_.Ref;
    return true;
  }

  public override bool _ProcessLeap(double integrateTime) {
    Position = record_.Ref;
    return true;
  }
}

/**
 * leap 0 の間だけ動き、leap した後は止まる検体。往復性は保っている。
 *
 * leap 0 の区間では動いているので、「どれか 1 つの leap で動いていれば足りる」という
 * 見方だと、leap 1 の区間で静止物を比べているだけなのを見逃す。
 */
public partial class StopsAfterLeapProbe : ReversibleNode3D {
  private static readonly Vector3 Velocity = new(1.0f, 0.0f, 0.0f);
  private Dense<Vector3> record_;

  public override void _Ready() {
    base._Ready();
    record_ = new Dense<Vector3>(Clock, Vector3.Zero);
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var position = ref record_.Mut;
    if (Clock.CurrentLeap == 0) {
      position += Velocity * (float)dt;
    }
    Position = position;
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    Position = record_.Ref;
    return true;
  }

  public override bool _ProcessLeap(double integrateTime) {
    Position = record_.Ref;
    return true;
  }
}

/**
 * まったく動かない検体。往復性は自明に成り立つが、成り立たせているのは
 * 「何も起きていない」ことなので、ハーネスはこれを「検査できていない」と判定しなければならない。
 */
public partial class StationaryProbe : ReversibleNode3D {
  public override bool _ProcessForward(double integrateTime, double dt) => true;

  public override bool _ProcessBack(double integrateTime) => true;
}
