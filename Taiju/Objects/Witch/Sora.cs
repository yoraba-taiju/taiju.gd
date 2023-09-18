using System;
using Godot;

namespace Taiju.Objects.Witch;

public partial class Sora : Node3D {
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
  }

  private const double MoveDelta = 12.0;

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
    var newPos = Position + pos;
    newPos.X = Math.Clamp(newPos.X, -22.0f, 22.0f);
    newPos.Y = Math.Clamp(newPos.Y, -12.0f, 12.0f);
    Position = newPos;
  }
}
