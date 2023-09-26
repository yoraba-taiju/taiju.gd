using System;
using Godot;
using Taiju.Reversible.GD;
using Taiju.Reversible.Value;

namespace Taiju.Objects.Witch;

public partial class Sora : ReversibleNode3D {
  private const double MoveDelta = 12.0;
  private Dense<Vector3> position_;

  public override void _Ready() {
    base._Ready();
    position_ = new Dense<Vector3>(Clock, new Vector3(0f, 0f, 0f));
  }

  public override void _Process(double delta) {
    var deltaPos = new Vector3();
    var moved = false;
    if (Input.IsActionPressed("move_right")) {
      deltaPos.X += 1;
      moved = true;
    }
    if (Input.IsActionPressed("move_left")) {
      deltaPos.X -= 1;
      moved = true;
    }
    if (Input.IsActionPressed("move_up")) {
      deltaPos.Y += 1;
      moved = true;
    }
    if (Input.IsActionPressed("move_down")) {
      deltaPos.Y -= 1;
      moved = true;
    }

    if (!moved) {
      return;
    }

    deltaPos = deltaPos.Normalized() * (float)(delta * MoveDelta);
    ref var pos = ref position_.Mut;
    pos += deltaPos;
    pos.X = Math.Clamp(pos.X, -21.0f, 21.0f);
    pos.Y = Math.Clamp(pos.Y, -11.5f, 11.5f);

    // Update using current value.
    Position = pos;
  }
}
