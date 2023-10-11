﻿using Godot;
using Taiju.Reversible.Gd.Companion;

namespace Taiju.Reversible.Gd;

public abstract partial class ReversibleNode3D : Node3D, IReversibleNode {
  private ReversibleCompanion comp_;

  /*
   * Members
   */
  protected Clock Clock => comp_.Clock;

  public override void _Ready() {
    comp_.Ready(this);
  }

  /*
   * Impls
   */
  public override void _Process(double delta) {
    comp_.Process(this, delta);
  }
  
  /*
   * Helpers
   */

  protected void Destroy(double after) {
    comp_.Destroy(this, (uint)Math.Ceiling(after / ClockNode.TickTime));
  }

  protected void Destroy() {
    comp_.Destroy(this, 0);
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
