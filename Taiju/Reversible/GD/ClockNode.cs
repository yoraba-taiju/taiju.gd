using Godot;

namespace Taiju.Reversible.GD; 

public partial class ClockNode : Node3D {
  public Clock Clock { get; private set; }

  public override void _Ready() {
    Clock = new Clock();
  }
}
