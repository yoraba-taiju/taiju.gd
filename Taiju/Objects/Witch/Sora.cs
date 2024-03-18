using System;
using Godot;
using Taiju.Objects.Effect;
using Taiju.Objects.Enemy;
using Taiju.Objects.Reversible.Godot;
using Taiju.Objects.Reversible.Value;

namespace Taiju.Objects.Witch;

public partial class Sora : ReversibleRigidBody3D {
  [Export(PropertyHint.Range, "0,0.2,")] private double nextBulletTime_ = 0.08;
  private SoraBulletServer bulletServer_;
  private Node3D bulletNode_;
  private PackedScene arrowAsset_;
  private const double MoveDelta = 16.0;

  private record struct Record {
    public Vector3 Position;
    public double SpiritRot;
    public double AfterFire;
  }
  private Dense<Record> record_;
  
  // Nodes
  private Node3D spirit_;

  public override void _Ready() {
    base._Ready();
    bulletServer_ = GetNode<SoraBulletServer>("/root/Root/Field/WitchBullet/SoraBulletServer")!;
    bulletNode_ = GetNode<Node3D>("/root/Root/Field/WitchBullet")!;
    arrowAsset_ = ResourceLoader.Load<PackedScene>("res://Objects/Effect/Arrow.tscn")!;
    // Initial State
    record_ = new Dense<Record>(Clock, new Record {
      Position = Position,
      SpiritRot = 0.0,
      AfterFire = 0.0,
    });
    spirit_ = GetNode<Node3D>("Spirit")!;
    ContactMonitor = true;
    MaxContactsReported = 1;
    BodyEntered += OnBodyEntered;
  }

  private void OnBodyEntered(Node node) {
    if (node is not EnemyBase) {
      return;
    }
    Hit();
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var rec = ref record_.Mut;

    // Position
    ref var pos = ref rec.Position;
    var deltaPos = new Vector3();
    var moved = false;
    if (Input.IsActionPressed("move_right")) {
      deltaPos.X += 1.0f;
      moved = true;
    }
    if (Input.IsActionPressed("move_left")) {
      deltaPos.X -= 1.0f;
      moved = true;
    }
    if (Input.IsActionPressed("move_up")) {
      deltaPos.Y += 1.0f;
      moved = true;
    }
    if (Input.IsActionPressed("move_down")) {
      deltaPos.Y -= 1.0f;
      moved = true;
    }

    if (moved) {
      deltaPos = deltaPos.Normalized() * (float)(dt * MoveDelta);
      pos += deltaPos;
      pos.X = Mathf.Clamp(pos.X, -21.0f, 21.0f);
      pos.Y = Mathf.Clamp(pos.Y, -11.5f, 11.5f);
    }

    // Spirit rot
    ref var rot = ref rec.SpiritRot;
    rot += dt;

    // Handle shot
    ref var afterFire = ref rec.AfterFire;
    if (Input.IsActionJustPressed("fire")) {
      bulletServer_.Spawn(pos);
      afterFire = nextBulletTime_;
    } else if (Input.IsActionPressed("fire")) {
      afterFire -= dt;
      if (afterFire < 0.0) {
        bulletServer_.SpawnDouble(pos);
        afterFire += nextBulletTime_;
      }
    } else {
      afterFire = nextBulletTime_;
    }

    if (Input.IsActionJustPressed("spell")) {
      InvokeArrow();
    }
    
    // Update using current value.
    Position = pos;
    spirit_.Rotation = new Vector3(0.0f, (float)rot, 0.0f);
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    return LoadCurrentStatus();
  }

  private void InvokeArrow() {
    var arrow = arrowAsset_.Instantiate<Arrow>();
    arrow.InitialPosition = Position;
    arrow.InitialVelocity = Vector3.Left * 120.0f;
    bulletNode_.AddChild(arrow);
  }

  private bool LoadCurrentStatus() {
    ref readonly var state = ref record_.Ref;
    ref readonly var pos = ref state.Position;
    ref readonly var rot = ref state.SpiritRot;
    Position = pos;
    spirit_.Rotation = new Vector3(0.0f, (float)rot, 0.0f);
    return true;
  }

  public void Hit() {
    Console.WriteLine("Hit");
  }
}
