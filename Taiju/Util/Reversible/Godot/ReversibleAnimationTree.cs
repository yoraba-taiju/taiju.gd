using System;
using Godot;
using Taiju.Objects;
using Taiju.Util.Reversible.Godot.Companion;

namespace Taiju.Util.Reversible.Godot;

public partial class ReversibleAnimationTree : AnimationTree, IReversibleNode {
  private ReversibleCompanion<ReversibleAnimationTree> comp_;

  /*
   * Members
   */
  protected Clock Clock => comp_.Clock;
  protected ClockNode ClockNode => comp_.ClockNode;
  public bool IsAlive {
    get => comp_.IsAlive;
    set => comp_.IsAlive = value;
  }

  public override void _Ready() {
    comp_.Ready(this);
    Active = true;
  }

  protected virtual void Seek(double t) {
    Set("parameters/Seek/seek_request", t);
  }

  /*
   * Impls
   */
  public override void _Process(double delta) {
    comp_.Process(this, delta);
    switch (ClockNode.Direction) {
      case ClockNode.TimeDirection.Forward:
        Active = true;
        break;
      case ClockNode.TimeDirection.Back:
        Active = false;
        break;
      case ClockNode.TimeDirection.Stop:
        Active = false;
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  /*
   * Helpers
   */

  public void Destroy() {
    comp_.Destroy(this);
  }

  public void Rescue() {
    comp_.Rescue(this);
  }

  /*
   * Default overrides
   */

  public virtual bool _ProcessForward(double integrateTime, double dt) {
    return true;
  }

  public virtual bool _ProcessBack(double integrateTime) {
    return true;
  }

  public virtual bool _ProcessLeap(double integrateTime) {
    return true;
  }

  public virtual void _ProcessRaw(double integrateTime, double dt) {}
  public virtual void _OnDestroy() {}
  public virtual void _OnRescue() {}
}
