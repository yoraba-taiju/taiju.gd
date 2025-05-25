using System;
using Godot;
using Taiju.Util.Reversible.Value;
using Taiju.Objects.Rush;

namespace Taiju.Util.Reversible.Godot.Companion;

public struct ReversibleCompanion<T>
  where T: Node, IReversibleNode
{
  /// Accessors
  public ClockNode ClockNode { get; private set; }
  public Clock Clock { get; private set; }
  /// Clock Stats
  public bool IsAlive { get; set; }
  private ClockNode.TimeDirection Direction => ClockNode.Direction;
  private bool Ticked => ClockNode.Ticked;
  private bool Leap => ClockNode.Leaped;

  /// This object
  private uint bornTick_;
  private Dense<double> integrateTime_;
  
  /**
   * Impls
   */

  public void Ready(T self) {
    ClockNode = self.GetNode<ClockNode>("/root/Root/Clock");
    Clock = ClockNode.Clock;
    bornTick_ = Clock.CurrentTick;
    integrateTime_ = new Dense<double>(Clock, 0.0);
    IsAlive = true;
  }

  public void Process(T self, double delta) {
    // Destroy myself if backed before born.
    if (Clock.CurrentTick < bornTick_) {
      self.QueueFree();
      return;
    }

    // Run
    switch (Direction) {
      case ClockNode.TimeDirection.Stop: {
        ref readonly var integrateTime = ref integrateTime_.Ref;
        if (Leap) {
          if (self._ProcessLeap(integrateTime)) {
            return;
          }
          self._ProcessRaw(integrateTime, delta);
        }
      }
        break;

      case ClockNode.TimeDirection.Forward: {
        ref var integrateTime = ref integrateTime_.Mut;
        integrateTime += delta;
        if (self._ProcessForward(integrateTime, delta)) {
          return;
        }
        self._ProcessRaw(integrateTime, delta);
      }
        break;

      case ClockNode.TimeDirection.Back: {
        ref readonly var integrateTime = ref integrateTime_.Ref;
        if (self._ProcessBack(integrateTime)) {
          return;
        }
        self._ProcessRaw(integrateTime, delta);
        break;
      }

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
    if (self is not RushBase) {
      self.PropagateCall("_OnDestroy", null, true);
    }
    self.SetDeferred(Node3D.PropertyName.Visible, false);
    self.SetDeferred(Node.PropertyName.ProcessMode, (int)Node.ProcessModeEnum.Disabled);
  }

  public void Rescue(T self) {
    if (IsAlive) {
      return;
    }
    IsAlive = true;
    if (self is not RushBase) {
      self.PropagateCall("_OnRescue", null, true);
    }
    self.SetDeferred(Node3D.PropertyName.Visible, true);
    self.SetDeferred(Node.PropertyName.ProcessMode, (int)Node.ProcessModeEnum.Inherit);
  }
}
