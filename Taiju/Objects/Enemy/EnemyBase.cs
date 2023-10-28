using Godot;
using Taiju.Objects.Effect;
using Taiju.Objects.Witch;
using Taiju.Reversible.Gd;

namespace Taiju.Objects.Enemy; 

public abstract partial class EnemyBase : ReversibleRigidBody3D {
  private PackedScene explosionScene_;
  protected Sora Sora { get; private set; }

  protected int Shield;
  public override void _Ready() {
    base._Ready();
    explosionScene_ = ResourceLoader.Load<PackedScene>("res://Objects/Effect/RevesibleExplosion.tscn");
    Sora = GetNode<Sora>("/root/Root/Field/Witch/Sora");
    Shield = 1;
  }

  public void Hit() {
    Shield -= 1;
    if (Shield > 0) {
      return;
    }

    Destroy();
    var explosion = explosionScene_.Instantiate<ReversibleExplosion>();
    explosion.Position = Position;
    GetParent().AddChild(explosion);
  }
}
