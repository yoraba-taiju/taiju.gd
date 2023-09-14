using Godot;

namespace Taiju.Objects.Witch;

public partial class Sora : Node3D {
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta) {
    var vel = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
    Position = Position + new Vector3(vel.X, vel.Y, 0.0f);
  }
}
