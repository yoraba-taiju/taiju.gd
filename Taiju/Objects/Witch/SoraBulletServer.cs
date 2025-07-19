using Godot;
using Taiju.Objects.Effect;
using Taiju.Scenes.Stages;
using Taiju.Util.Reversible.Godot;

namespace Taiju.Objects.Witch; 

public partial class SoraBulletServer : ReversibleNode3D {
  private Stager stager_;
  private ResourceManager resourceManager_;
  private PackedScene bulletScene_;
  private Node3D witchEffect_;
  private PackedScene hitEffectScene_;

  public override void _Ready() {
    base._Ready();
    stager_ = GetNode<Stager>("/root/Root/Stager")!;
    resourceManager_ = stager_.ResourceManager;
    bulletScene_ = resourceManager_.Load<PackedScene>("res://Objects/Witch/SoraBullet.tscn")!;
    witchEffect_ = GetNode<Node3D>("/root/Root/Field/WitchEffect")!;
    hitEffectScene_ = resourceManager_.Load<PackedScene>("res://Objects/Effect/HitSparkle_SoraBullet.tscn")!;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    return false;
  }

  public override bool _ProcessBack(double integrateTime) {
    return false;
  }

  public void Shot(Vector3 pos) {
    var bullet = bulletScene_.Instantiate<SoraBullet>();
    bullet.Position = pos;
    bullet.Damage = 2;
    AddChild(bullet);
  }

  public void ShotDouble(Vector3 pos) {
    {
      var bullet = bulletScene_.Instantiate<SoraBullet>();
      bullet.Position = pos + Vector3.Up / 1.7f;
      bullet.Damage = 1;
      AddChild(bullet);
    }
    {
      var bullet = bulletScene_.Instantiate<SoraBullet>();
      bullet.Position = pos + Vector3.Down / 1.7f;
      bullet.Damage = 1;
      AddChild(bullet);
    }
  }

  public void SpawnSparkle(Vector3 pos) {
    var effect = hitEffectScene_.Instantiate<HitSparkle>();
    effect.Position = pos;
    witchEffect_.AddChild(effect);
  }
}
