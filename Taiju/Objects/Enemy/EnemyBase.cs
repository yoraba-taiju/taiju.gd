using Godot;
using Taiju.Objects.Effect;
using Taiju.Objects.Witch;
using Taiju.Objects.Reversible.Godot;

namespace Taiju.Objects.Enemy; 

public abstract partial class EnemyBase : ReversibleRigidBody3D {
  private PackedScene explosionScene_;
  private Node3D effectNode_;
  protected Sora Sora { get; private set; }
  private bool displayed_;

  public override void _Ready() {
    base._Ready();
    explosionScene_ = ResourceLoader.Load<PackedScene>("res://Objects/Effect/RevesibleExplosion.tscn");
    effectNode_ = GetNode<Node3D>("/root/Root/Field/EnemyEffect");
    Sora = GetNode<Sora>("/root/Root/Field/Witch/Sora");
    displayed_ = false;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    if (Mathf.Abs(Position.X) <= 21.0f || Mathf.Abs(Position.Y) <= 11.5f) {
      displayed_ = true;
    } else if (displayed_ && (Mathf.Abs(Position.X) >= 24.0f || Mathf.Abs(Position.Y) >= 13.5f)) {
      Destroy();
    }
    return true;
  }

  public abstract void Hit();

  public override void Destroy() {
    base.Destroy();
    var explosion = explosionScene_.Instantiate<ReversibleExplosion>();
    explosion.Position = Position;
    effectNode_.AddChild(explosion);
  }
}
