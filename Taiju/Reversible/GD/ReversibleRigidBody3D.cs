using Godot;

namespace Taiju.Reversible.GD; 

public abstract partial class ReversibleRigidBody3D : RigidBody3D, IReversibleNode {
  private ClockNode clockNode_;
  private Clock clock_;

  protected ClockNode ClockNode => clockNode_;
  protected Clock Clock => clock_;
  
  public override void _Ready() {
    clockNode_ = GetNode<ClockNode>("/root/Root/Clock");
    clock_ = clockNode_.Clock;
  }
}
