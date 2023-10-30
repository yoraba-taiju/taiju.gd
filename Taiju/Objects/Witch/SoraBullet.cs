using System;
using Godot;
using Taiju.Objects.Enemy;
using Taiju.Reversible.Gd;
using Taiju.Reversible.Value;

namespace Taiju.Objects.Witch; 

public partial class SoraBullet : ReversibleRigidBody3D {
  private Vector3 spawnPoint_;
  private Area3D area_;
  private Dense<Record> record_;
  private struct Record {
    public Vector3 Position;
  }
  public override void _Ready() {
    base._Ready();
    spawnPoint_ = Position;
    area_ = GetNode<Area3D>("Collider");
    area_.BodyEntered += OnCollide;
    record_ = new Dense<Record>(Clock, new Record {
      Position = Position,
    });
  }

  private void OnCollide(Node3D node) {
    if (node is not EnemyBase enemy) {
      return;
    }
    enemy.Hit();
    Destroy();
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    state.LinearVelocity = Vector3.Right * 60.0f;
  }

  private void OnForward() {
    ref var state = ref record_.Mut;
    ref var pos = ref state.Position;
    
    if (Mathf.Abs(pos.X) >= 21.00001f || Mathf.Abs(pos.Y) >= 11.50001f) {
      Destroy();
    }

    // Record
    pos = Position;
  }

  private void OnBack() {
    ref readonly var state = ref record_.Ref;
    ref readonly var pos = ref state.Position;
    Position = pos;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    OnForward();
    return false;
  }

  public override bool _ProcessBack(double integrateTime) {
    OnBack();
    return true;
  }

  public override bool _ProcessLeap(double integrateTime) {
    OnBack();
    return true;
  }
}
