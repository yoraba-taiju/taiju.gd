using Godot;

namespace Taiju.Objects.Witch;

public partial class Sora : Node3D {
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
  }

  private const double MoveDelta = 8.0;

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta) {
    var pos = new Vector3();
    var moved = false;
    if (Input.IsActionPressed("move_right")) {
      pos.X += 1;
      moved = true;
    }
    if (Input.IsActionPressed("move_left")) {
      pos.X -= 1;
      moved = true;
    }
    if (Input.IsActionPressed("move_up")) {
      pos.Y += 1;
      moved = true;
    }
    if (Input.IsActionPressed("move_down")) {
      pos.Y -= 1;
      moved = true;
    }

    if (!moved) {
      return;
    }

    pos = pos.Normalized() * (float)(delta * MoveDelta);
    Position += pos;
  }
}
