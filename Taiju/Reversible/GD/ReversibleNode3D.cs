using Godot;

namespace Taiju.Reversible.GD; 

public partial class ReversibleNode3D : Node3D {
  private ClockNode clockNode_;

  protected Clock Clock => ClockNode.Clock;
  protected ClockNode ClockNode => clockNode_;
  
  public override void _Ready() {
    clockNode_ = GetNode<ClockNode>("/root/Root/Clock");
  }
}
