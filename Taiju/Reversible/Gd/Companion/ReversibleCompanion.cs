using System;
using Godot;

namespace Taiju.Reversible.Gd.Companion;

public struct ReversibleCompanion {
  /// Accessors
  public ClockNode ClockNode { get; private set; }
  public Clock Clock { get; private set; }
  /// Clock Stats
  private double ClockIntegrateTime => ClockNode.IntegrateTime;
  
  private ClockNode.TimeDirection Direction => ClockNode.Direction;
  private bool Ticked => ClockNode.Ticked;
  private bool Leap => ClockNode.Leaped;

  /// Lifetime
  private bool destoryed_;
  private uint destroyedAt_;

  /// This object
  private double bornAt_;
  
  /**
   * Impls
   */

  public void Ready(Node3D self) {
    ClockNode = self.GetNode<ClockNode>("/root/Root/Clock");
    Clock = ClockNode.Clock;
    bornAt_ = ClockIntegrateTime;
    destoryed_ = false;
    destroyedAt_ = uint.MaxValue;
  }

  public void Process(Node3D selfAsNode3D, double delta) {
    if (destoryed_) {
      var currentTick = Clock.CurrentTick;
      if (destroyedAt_ + Clock.HistoryLength <= currentTick) {
        // Vanish self.
        selfAsNode3D.QueueFree();
        return;
      }
      if (!(currentTick < destroyedAt_)) {
        // When destroy queued.
        return;
      }
      Rebirth(selfAsNode3D);
    }

    var self = (IReversibleNode) selfAsNode3D;
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

  public void Destroy(Node3D self) {
    destoryed_ = false;
    destroyedAt_ = Clock.CurrentTick;
    self.Visible = false;
  }

  private void Rebirth(Node3D self) {
    destoryed_ = false;
    destroyedAt_ = uint.MaxValue;
    self.Visible = true;
  }
}
