﻿using System;
using Godot;

namespace Taiju.Objects.Reversible.Godot.Companion;

public struct ReversibleCompanion<T>
  where T: Node, IReversibleNode
{
  /// Accessors
  public ClockNode ClockNode { get; private set; }
  public Clock Clock { get; private set; }
  /// Clock Stats
  private double ClockIntegrateTime => ClockNode.IntegrateTime;
  public bool IsAlive { get; set; }
  private ClockNode.TimeDirection Direction => ClockNode.Direction;
  private bool Ticked => ClockNode.Ticked;
  private bool Leap => ClockNode.Leaped;

  /// This object
  private uint bornTick_;
  private double bornAt_;
  
  /**
   * Impls
   */

  public void Ready(T self) {
    ClockNode = self.GetNode<ClockNode>("/root/Root/Clock");
    Clock = ClockNode.Clock;
    bornTick_ = Clock.CurrentTick;
    bornAt_ = ClockIntegrateTime;
    IsAlive = true;
  }

  public void Process(T self, double delta) {
    if (Clock.CurrentTick < bornTick_) {
      self.QueueFree();
      return;
    }
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

  public void Destroy(T self) {
    if (!IsAlive) {
      return;
    }
    ClockNode.QueueDestroy(self);
    IsAlive = false;
    self.SetDeferred(Node3D.PropertyName.Visible, false);
    self.SetDeferred(Node.PropertyName.ProcessMode, (int)Node.ProcessModeEnum.Disabled);
  }

  public void Rescue(T self) {
    if (IsAlive) {
      return;
    }
    IsAlive = true;
    self.SetDeferred(Node3D.PropertyName.Visible, true);
    self.SetDeferred(Node.PropertyName.ProcessMode, (int)Node.ProcessModeEnum.Inherit);
  }
}
