using System;
using Godot;
using Taiju.Reversible.GD.Companion;

namespace Taiju.Reversible.GD;

public abstract partial class ReversibleNode3D : Node3D, IReversibleNode {
  private ReversibleCompanion comp_;

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
