using System;
using Godot;

namespace Taiju.Reversible.GD; 

public abstract partial class ReversibleRigidBody3D : RigidBody3D, IReversibleNode {
  /// Members
  private ClockNode clockNode_;
  private Clock clock_;

  /// Accessors
  protected ClockNode ClockNode => clockNode_;
  protected Clock Clock => clock_;
  
  /// Clock Stats
  private double ClockIntegrateTime => clockNode_.IntegrateTime;
  protected ClockNode.TimeDirection Direction => ClockNode.Direction;
  protected bool Ticked => ClockNode.Ticked;
  protected bool Leap => ClockNode.Leaped;

  /// This object
  private double bornAt_;

  /// Impls
  public override void _Ready() {
    clockNode_ = GetNode<ClockNode>("/root/Root/Clock");
    clock_ = clockNode_.Clock;
    bornAt_ = ClockIntegrateTime;
  }
  public override void _Process(double delta) {
    var integrateTime = ClockIntegrateTime - bornAt_;
    switch (Direction) {
      case ClockNode.TimeDirection.Stop:
        if (Leap) {
          if (_ProcessLeap()) {
            return;
          }
          _ProcessRaw(integrateTime);
        }
        break;
      case ClockNode.TimeDirection.Forward:
        if (_ProcessForward(integrateTime, delta)) {
          return;
        }
        _ProcessRaw(integrateTime);
        break;
      case ClockNode.TimeDirection.Back:
        if (_ProcessBack()) {
          return;
        }
        _ProcessRaw(integrateTime);
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
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
