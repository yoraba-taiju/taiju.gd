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
    area_.BodyEntered += area => {
      if (area is EnemyBase) {
        var enemy = (EnemyBase)area;
        enemy.Hit();
        Destroy();
      }
    };
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    return false;
  }

  public override bool _ProcessBack(double integrateTime) {
    return false;
  }
}
