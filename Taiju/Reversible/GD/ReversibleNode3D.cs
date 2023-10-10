using System;
using Godot;

namespace Taiju.Reversible.GD; 

public abstract partial class ReversibleNode3D : Node3D, IReversibleNode {
  /// Members
  private ClockNode clockNode_;
  private Clock clock_;

  /// Accessors
  protected ClockNode ClockNode => clockNode_;
  protected Clock Clock => clock_;
  /// Clock Stats
  public double IntegrateTime => clockNode_.IntegrateTime;
  protected bool Forward => ClockNode.Forward;
  protected bool Back => ClockNode.Forward;
  protected bool Ticked => ClockNode.Ticked;
  protected bool Leap => ClockNode.Leaped;

  /// This object
  private double bornAt_;

  public override void _Ready() {
    clockNode_ = GetNode<ClockNode>("/root/Root/Clock");
    clock_ = clockNode_.Clock;
    bornAt_ = IntegrateTime;
  }

  /// Impls
  public override void _Process(double delta) {
    var integrateTime = IntegrateTime - bornAt_;
    if (Forward) {
      if (_ProcessForward(integrateTime, delta)) {
        return;
      }
      _ProcessRaw(integrateTime);
      return;
    }

    if (Back) {
      if (_ProcessBack()) {
        return;
      }
      _ProcessRaw(integrateTime);
      return;
    }

    if (Leap) {
      if (_ProcessLeap()) {
        return;
      }
      _ProcessRaw(integrateTime);
    }
    throw new NotImplementedException("???");
  }

  /*
   * Default overrides
   */

  public bool _ProcessForward(double integrateTime, double dt) {
    return true;
  }

  public bool _ProcessBack() {
    return true;
  }

  public bool _ProcessLeap() {
    return true;
  }
  
  public void _ProcessRaw(double integrateTime) {
  }
}
