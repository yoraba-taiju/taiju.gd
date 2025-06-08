using System;
using Godot;
using Taiju.Objects.Effect;
using Taiju.Objects.Witch;
using Taiju.Util.Reversible.Godot;

namespace Taiju.Objects.Enemy; 

public abstract partial class EnemyBase : ReversibleRigidBody3D {
  [Export(PropertyHint.Range, "0,100,1")] protected int InitialShield = 2;
  [Export] protected long Score = 100;
  [Export] protected Vector2 BodySize = new(0, 0);
  [Export] protected float ExplosionScale = 1.0f;
  private PackedScene explosionPack_;
  private PackedScene magicElementItemPack_;
  private Node3D effectNode_;
  protected Player Player;
  protected Sora Sora { get; private set; }
  private bool displayed_;

  public override void _Ready() {
    base._Ready();
    explosionPack_ = ResourceLoader.Load<PackedScene>("res://Objects/Effect/ReversibleExplosion.tscn")!;
    magicElementItemPack_ = ResourceLoader.Load<PackedScene>("res://Objects/Effect/MagicElementItem.tscn")!;
    effectNode_ = GetNode<Node3D>("/root/Root/Field/EnemyEffect")!;
    Player = GetNode<Player>("/root/Root/Player")!;
    Sora = GetNode<Sora>("/root/Root/Field/Witch/Sora")!;
    displayed_ = false;
  }
  
  public override bool _ProcessForward(double integrateTime, double dt) {
    var halfSize = BodySize / 2.0f;
    switch (displayed_) {
      case false when !(Mathf.Abs(Position.X) >= 22.0f + halfSize.X || Mathf.Abs(Position.Y) >= 11.5f + halfSize.Y):
        displayed_ = true;
        break;
      case true when Mathf.Abs(Position.X) >= 24.0f + halfSize.X || Mathf.Abs(Position.Y) >= 13.5f + halfSize.Y:
        Destroy();
        break;
    }
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    var halfSize = BodySize / 2.0f;
    if (displayed_ && (Mathf.Abs(Position.X) >= 22.0f + halfSize.X || Mathf.Abs(Position.Y) >= 11.5f + halfSize.Y)) {
      displayed_ = false;
    }
    return true;
  }

  public bool Hit(int damage) {
    if (!IsAlive) {
      return false;
    }
    ref var shield = ref ShieldMut;
    shield -= damage;
    if (shield > 0) {
      return true;
    }
    Player.OnScoreAdded(Score);
    ExplodeAndDestroy();
    return false;
  }

  protected abstract ref int ShieldMut { get; }

  private void ExplodeAndDestroy() {
    Destroy();
    var scale = Vector3.One * ExplosionScale;
    {
      var explosion = explosionPack_.Instantiate<ReversibleExplosion>();
      explosion.Position = Position;
      explosion.Scale = scale;
      effectNode_.AddChild(explosion);
    }
    {
      var max = Math.Max(InitialShield / 4, 1);
      for (var i = 0; i < max; i++) {
        var magicElement = magicElementItemPack_.Instantiate<MagicElementItem>();
        magicElement.Position = Position;
        magicElement.Scale = scale;
        effectNode_.AddChild(magicElement);
      }
    }
  }
}
