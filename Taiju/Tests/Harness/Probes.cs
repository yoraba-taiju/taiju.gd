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
 * まったく動かない検体。往復性は自明に成り立つが、成り立たせているのは
 * 「何も起きていない」ことなので、ハーネスはこれを「検査できていない」と判定しなければならない。
 */
public partial class StationaryProbe : ReversibleNode3D {
  public override bool _ProcessForward(double integrateTime, double dt) => true;

  public override bool _ProcessBack(double integrateTime) => true;
}
