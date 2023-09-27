using Godot;

namespace Taiju.Reversible.GD; 

public abstract partial class ReversibleRigidBody3D : RigidBody3D, IReversibleNode {
  private ClockNode clockNode_;

  protected Clock Clock => ClockNode.Clock;
  protected ClockNode ClockNode => clockNode_;
  
  public override void _Ready() {
    clockNode_ = GetNode<ClockNode>("/root/Root/Clock");
  }
}
