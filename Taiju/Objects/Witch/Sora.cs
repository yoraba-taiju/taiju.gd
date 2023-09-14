using System;
using Godot;

namespace Taiju.Objects.Witch;

public partial class Sora : Node3D {
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
  }

  private const double MoveDelta = 30.0;

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta) {
    var pos = new Vector3();
    if (Input.IsActionPressed("move_right")) {
      pos.X += (float)(delta * MoveDelta);
    }
    if (Input.IsActionPressed("move_left")) {
      pos.X -= (float)(delta * MoveDelta);
    }
    if (Input.IsActionPressed("move_up")) {
      pos.Y += (float)(delta * MoveDelta);
    }
    if (Input.IsActionPressed("move_down")) {
      pos.Y -= (float)(delta * MoveDelta);
    }

    Position += pos;
  }
}
