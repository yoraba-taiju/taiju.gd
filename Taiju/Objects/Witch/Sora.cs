using System;
using Godot;
using Taiju.Reversible.Gd;
using Taiju.Reversible.Value;
using Vector3 = Godot.Vector3;

namespace Taiju.Objects.Witch;

public partial class Sora : ReversibleNode3D {
  [Export] private SoraBulletServer bulletServer_;
  private const double MoveDelta = 16.0;
  private record struct Record {
    public Vector3 Position;
    public double SpiritRot;
  }
  private Dense<Record> state_;
  
  // Nodes
  private Node3D spirit_;

  public override void _Ready() {
    base._Ready();
    state_ = new Dense<Record>(Clock, new Record {
      Position = Position,
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

    // Handle shot
    if (Input.IsActionJustPressed("fire")) {
      bulletServer_.Spawn(pos);
    }
    
    // Update using current value.
    Position = pos;
    spirit_.Rotation = new Vector3(0.0f, (float)rot, 0.0f);
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    return LoadCurrentStatus();
  }

  private bool LoadCurrentStatus() {
    ref readonly var state = ref state_.Ref;
    ref readonly var pos = ref state.Position;
    ref readonly var rot = ref state.SpiritRot;
    Position = pos;
    spirit_.Rotation = new Vector3(0.0f, (float)rot, 0.0f);
    return true;
  }
}
