using Godot;
using Taiju.Reversible.Gd;

namespace Taiju.Objects.Witch; 

public partial class SoraBulletServer : ReversibleNode3D {
  [Export] private PackedScene bulletScene_;
  public override bool _ProcessForward(double integrateTime, double dt) {
    throw new System.NotImplementedException();
  }

  public override bool _ProcessBack(double integrateTime) {
    throw new System.NotImplementedException();
  }

  public void Spawn(Vector3 pos) {
    var bullet = bulletScene_.Instantiate<SoraBullet>();
    bullet.Position = pos;
    AddChild(bullet);
  }
}
