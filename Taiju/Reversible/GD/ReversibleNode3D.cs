using System;
using Godot;

namespace Taiju.Reversible.GD;

public abstract partial class ReversibleNode3D : Node3D, IReversibleNode {
  private ReversibleHelper helper_;

  public override void _Ready() {
    helper_.Ready(this);
  }

  /// Impls
  public override void _Process(double delta) {
    helper_.Process(this, delta);
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
