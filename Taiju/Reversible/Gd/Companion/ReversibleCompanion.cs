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
  private enum LifeStatus {
    Living,
    DestroyQueued,
    Destroyed,
  }
  private LifeStatus lifeStatus_;
  private uint destroyQueuedAt_;
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
    lifeStatus_ = LifeStatus.Living;
    destroyedAt_ = uint.MaxValue;
  }

  public void Process(Node3D selfAsNode3D, double delta) {
    var currentTick = Clock.CurrentTick;
    switch (lifeStatus_) {
      case LifeStatus.Living:
        break;
      case LifeStatus.DestroyQueued:
        if (destroyedAt_ < currentTick) {
          lifeStatus_ = LifeStatus.Destroyed;
          selfAsNode3D.Visible = false;
          return;
        }
        if (currentTick <= destroyQueuedAt_) {
          lifeStatus_ = LifeStatus.Living;
          destroyQueuedAt_ = uint.MaxValue;
          destroyedAt_ = uint.MaxValue;
          selfAsNode3D.Visible = true;
        }
        break;
      case LifeStatus.Destroyed:
        if (destroyedAt_ + Clock.HistoryLength < currentTick) {
          // Vanish self.
          selfAsNode3D.QueueFree();
          return;
        }
        if (currentTick < destroyedAt_) {
          lifeStatus_ = LifeStatus.Living;
          destroyQueuedAt_ = uint.MaxValue;
          destroyedAt_ = uint.MaxValue;
          selfAsNode3D.Visible = true;
          break;
        }
        if (currentTick <= destroyQueuedAt_) {
          lifeStatus_ = LifeStatus.DestroyQueued;
          selfAsNode3D.Visible = true;
          return;
        }
        return;
      default:
        throw new ArgumentOutOfRangeException();
    }

    var self = (IReversibleNode) selfAsNode3D;
    var integrateTime = ClockIntegrateTime - bornAt_;
    switch (Direction) {
      case ClockNode.TimeDirection.Stop:
        if (Leap) {
          if (self._ProcessLeap(integrateTime)) {
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
        if (self._ProcessBack(integrateTime)) {
          return;
        }
        self._ProcessRaw(integrateTime);
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  public void Destroy(Node3D self, uint after) {
    destroyQueuedAt_ = Clock.CurrentTick;
    destroyedAt_ = Clock.CurrentTick + after;
    if (after == 0) {
      lifeStatus_ = LifeStatus.Destroyed;
      self.Visible = false;
    } else {
      lifeStatus_ = LifeStatus.DestroyQueued;
      self.Visible = true;
    }
  }
}
