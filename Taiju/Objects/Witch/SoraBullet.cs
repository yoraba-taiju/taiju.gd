using Godot;
using Taiju.Reversible.Gd;

namespace Taiju.Objects.Witch; 

public partial class SoraBullet : ReversibleRigidBody3D {
  private Vector3 spawnPoint_;
  public override void _Ready() {
    base._Ready();
    spawnPoint_ = Position;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    return false;
  }

  public override bool _ProcessBack(double integrateTime) {
    return false;
  }
}
