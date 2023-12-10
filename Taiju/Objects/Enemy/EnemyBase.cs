using Godot;
using Taiju.Objects.Effect;
using Taiju.Objects.Witch;
using Taiju.Objects.Reversible.Godot;

namespace Taiju.Objects.Enemy; 

public abstract partial class EnemyBase : ReversibleRigidBody3D {
  private PackedScene explosionScene_;
  private Node3D effectNode_;
  protected Sora Sora { get; private set; }

  protected int Shield;
  public override void _Ready() {
    base._Ready();
    explosionScene_ = ResourceLoader.Load<PackedScene>("res://Objects/Effect/RevesibleExplosion.tscn");
    effectNode_ = GetNode<Node3D>("/root/Root/Field/EnemyEffect");
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
    effectNode_.AddChild(explosion);
  }
}
