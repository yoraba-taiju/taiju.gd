using Godot;
using Taiju.Objects;
using Taiju.Objects.Effect;
using Taiju.Objects.Witch;
using Taiju.UI.HUD;
using Taiju.Util.Reversible.Godot;
using Taiju.Util.Reversible.Value;

namespace Taiju.Scenes.Stages;

public abstract partial class Stager : ReversibleNode3D {
  // Record
  private struct Record {
    public bool TransformChanged;
    public Transform3D Transform;
  }
  private Dense<Record> record_;

  // Nodes
  protected Sora Sora;
  protected Player Player;

  public override void _Ready() {
    base._Ready();
    record_ = new Dense<Record>(Clock, new Record {
      TransformChanged = false,
      Transform = Transform3D.Identity,
    });
    Sora = GetNode<Sora>("/root/Root/Field/Witch/Sora")!;
    Player = GetNode<Player>("/root/Root/Player")!;
  }

  protected void Move(Vector3 delta, double dt) {
    ref var rec = ref record_.Mut;
    rec.Transform = rec.Transform.TranslatedLocal(delta * (float)dt);
    rec.TransformChanged = true;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var rec = ref record_.Mut;
    if (rec.TransformChanged) {
      Transform = rec.Transform.Inverse();
      rec.TransformChanged = false;
    }
    return true;
  }

  public override bool _ProcessLeap(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    Transform = rec.Transform.Inverse();
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    Transform = rec.Transform.Inverse();
    return true;
  }
}
