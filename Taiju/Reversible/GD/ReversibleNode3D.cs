using Godot;

namespace Taiju.Reversible.GD; 

public partial class ReversibleNode3D : Node3D {
  [Export] private ClockNode clockNode_;

  protected ref Clock Clock => ref clockNode_.Clock;
  protected ClockNode ClockNode => clockNode_;

  public override void _Ready() {
    clockNode_ = GetNode<ClockNode>("/root/Root/Clock");
  }
}
