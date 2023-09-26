using System;
using Godot;
using Taiju.Reversible.GD;
using Taiju.Util.Value;

namespace Taiju.Objects.Witch;

public partial class Sora : ReversibleNode3D {

  public override void _Ready() {
    base._Ready();
  }

  private const double MoveDelta = 12.0;

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
    newPos.X = Math.Clamp(newPos.X, -21.0f, 21.0f);
    newPos.Y = Math.Clamp(newPos.Y, -11.5f, 11.5f);
    Position = newPos;
  }
}
