using System;
using Godot;

namespace Taiju.Reversible.GD; 

public struct ReversibleHelper {
  /// Members
  private ClockNode clockNode_;
  private Clock clock_;

  /// Accessors
  private ClockNode ClockNode => clockNode_;
  private Clock Clock => clock_;
  /// Clock Stats
  private double ClockIntegrateTime => clockNode_.IntegrateTime;
  
  private ClockNode.TimeDirection Direction => ClockNode.Direction;
  private bool Ticked => ClockNode.Ticked;
  private bool Leap => ClockNode.Leaped;

  /// This object
  private double bornAt_;
  
  /**
   * Impls
   */

  public void Ready(Node3D self) {
    clockNode_ = self.GetNode<ClockNode>("/root/Root/Clock");
    clock_ = clockNode_.Clock;
    bornAt_ = ClockIntegrateTime;
  }

  public void Process(IReversibleNode self, double delta) {
    var integrateTime = ClockIntegrateTime - bornAt_;
    switch (Direction) {
      case ClockNode.TimeDirection.Stop:
        if (Leap) {
          if (self._ProcessLeap()) {
            return;
          }
          self._ProcessRaw(integrateTime);
        }
        break;
      case ClockNode.TimeDirection.Forward:
        if (self._ProcessForward(integrateTime, delta)) {
          return;
        }
        self._ProcessRaw(integrateTime);
        break;
      case ClockNode.TimeDirection.Back:
        if (self._ProcessBack()) {
          return;
        }
        self._ProcessRaw(integrateTime);
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }
}
