using Godot;
using Taiju.Util.Reversible.Godot;
using Taiju.Util.Reversible.Value;

namespace Taiju.Scenes.Stages;

public partial class Stager : ReversibleNode3D {
  private struct Record {
    public Transform3D Transform;
  }

  private Dense<Record> rec_;

  public override void _Ready() {
    base._Ready();
    rec_ = new Dense<Record>(Clock, new Record {
      Transform = Transform3D.Identity,
    });
  }

  protected void Move(Vector3 delta, double dt) {
    ref var rec = ref rec_.Mut;
    rec.Transform = rec.Transform.TranslatedLocal(delta * (float)dt);
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref readonly var rec = ref rec_.Mut;
    Transform = rec.Transform.Inverse();
    return true;
  }

  public override bool _ProcessLeap(double integrateTime) {
    ref readonly var rec = ref rec_.Ref;
    Transform = rec.Transform.Inverse();
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    ref readonly var rec = ref rec_.Ref;
    Transform = rec.Transform.Inverse();
    return true;
  }
}
