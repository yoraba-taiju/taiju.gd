using Godot;
using Taiju.Objects.Enemy;
using Taiju.Reversible.Gd;
using Taiju.Reversible.Value;

namespace Taiju.Objects.Witch; 

public partial class SoraBullet : ReversibleRigidBody3D {
  private Vector3 spawnPoint_;
  private Area3D area_;
  private Dense<State> state_;
  private struct State {
    public Vector3 Position;
  }
  public override void _Ready() {
    base._Ready();
    spawnPoint_ = Position;
    area_ = GetNode<Area3D>("Collider");
    area_.BodyEntered += OnCollide;
    state_ = new Dense<State>(Clock, new State {
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
    state.LinearVelocity = Vector3.Right * 30.0f;
  }

  private void OnForward() {
    ref var state = ref state_.Mut;
    ref var pos = ref state.Position;
    pos = Position;
  }

  private void OnBack() {
    ref readonly var state = ref state_.Ref;
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
