using System;
using Godot;

namespace Taiju.Reversible.Gd.Companion;

public struct ReversibleCompanion<T>
  where T: Node3D, IReversibleNode
{
  /// Accessors
  public ClockNode ClockNode { get; private set; }
  public Clock Clock { get; private set; }
  /// Clock Stats
  private double ClockIntegrateTime => ClockNode.IntegrateTime;
    
  private ClockNode.TimeDirection Direction => ClockNode.Direction;
  private bool Ticked => ClockNode.Ticked;
  private bool Leap => ClockNode.Leaped;

  /// This object
  private double bornAt_;
  
  /**
   * Impls
   */

  public void Ready(Node3D self) {
    ClockNode = self.GetNode<ClockNode>("/root/Root/Clock");
    Clock = ClockNode.Clock;
    bornAt_ = ClockIntegrateTime;
  }

  public void Process(T node, double delta) {
    var self = (IReversibleNode)node;
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

  public void Destroy(Node3D self) {
    ClockNode.Destroy(self);
    self.Visible = false;
    self.SetDeferred("process_mode", (int)Node.ProcessModeEnum.Disabled);
  }

  public void Rescue(Node3D self) {
    self.Visible = true;
    self.SetDeferred("process_mode", (int)Node.ProcessModeEnum.Inherit);
  }
}
