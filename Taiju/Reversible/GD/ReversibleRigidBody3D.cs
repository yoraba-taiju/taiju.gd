using Godot;
using Taiju.Reversible.GD.Companion;

namespace Taiju.Reversible.GD;

public abstract partial class ReversibleRigidBody3D : RigidBody3D, IReversibleNode {
  private ReversibleCompanion comp_;

  /// Members
  protected Clock Clock => comp_.Clock;

  public override void _Ready() {
    comp_.Ready(this);
  }

  /// Impls
  public override void _Process(double delta) {
    comp_.Process(this, delta);
  }

  /*
   * Default overrides
   */

  public abstract bool _ProcessForward(double integrateTime, double dt);

  public abstract bool _ProcessBack();

  public virtual bool _ProcessLeap() {
    return true;
  }
  
  public virtual void _ProcessRaw(double integrateTime) {
  }
}
