using System;
using System.Numerics;
using Godot;
using Taiju.Reversible.GD;
using Taiju.Reversible.Value;
using Vector3 = Godot.Vector3;

namespace Taiju.Objects.Witch;

public partial class Sora : ReversibleNode3D {
  private const double MoveDelta = 16.0;
  private struct State {
    public Vector3 Position;
    public double SpiritRot;
  }
  private Dense<State> state_;
  
  // Nodes
  private Node3D spirit_;

  public override void _Ready() {
    base._Ready();
    state_ = new Dense<State>(Clock, new State {
      Position = Vector3.Zero,
      SpiritRot = 0.0,
    });
    spirit_ = GetNode<Node3D>("Spirit/Spirit");
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var state = ref state_.Mut;

    // Position
    ref var pos = ref state.Position;
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

    if (moved) {
      deltaPos = deltaPos.Normalized() * (float)(dt * MoveDelta);
      pos += deltaPos;
      pos.X = Math.Clamp(pos.X, -21.0f, 21.0f);
      pos.Y = Math.Clamp(pos.Y, -11.5f, 11.5f);
    }

    // Spirit rot
    ref var rot = ref state.SpiritRot;
    rot += dt;

    // Update using current value.
    Position = pos;
    spirit_.Rotation = new Vector3(0.0f, (float)rot, 0.0f);
    return base._ProcessForward(integrateTime, dt);
  }

  public override bool _ProcessBack() {
    ref readonly var state = ref state_.Ref;
    Position = state.Position;
    spirit_.Rotation = new Vector3(0.0f, (float)state.SpiritRot, 0.0f);
    return true;
  }
}
