using Godot;
using Taiju.Objects.Enemy;
using Taiju.Reversible.Gd;

namespace Taiju.Objects.Witch; 

public partial class SoraBullet : ReversibleRigidBody3D {
  private Vector3 spawnPoint_;
  private Area3D area_;
  public override void _Ready() {
    base._Ready();
    spawnPoint_ = Position;
    area_ = GetNode<Area3D>("Collider");
    area_.BodyEntered += OnCollide;
  }

  private void OnCollide(Node3D node) {
    if (node is not EnemyBase enemy) {
      return;
    }
    enemy.Hit();
    Destroy();
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    state.LinearVelocity = Vector3.Right * 30.0f;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    return false;
  }

  public override bool _ProcessBack(double integrateTime) {
    return false;
  }
}
