using System;
using Godot;
using Taiju.Reversible.GD;

namespace Taiju.Objects.Effect;

public partial class ReversibleExplosion : ReversibleParticle3D {
  [Export] private bool useRandomColor_;
  [Export] private Color color_ = Colors.Purple;
  [Export(PropertyHint.Range, "0.0, 1.0")] private float lifeTimeScale_ = 1.0f / 10.0f / 2.0f;
  protected override void _Emit(ref Item[] items, double integrateTime) {
    var rand = new RandomNumberGenerator();
    var zero = Transform2D.Identity;
    var maxSpeed = MaxSpeed;
    var meshes = Meshes;
    for (var i = 0; i < MeshCount; ++i) {
      ref var item = ref items[i];
      item.Color = useRandomColor_ ? Color.FromHsv(rand.Randf() * 360.0f, 0.5f, 0.9f) : color_;
      item.Velocity = maxSpeed * (rand.Randf() / 2.0f + 0.5f);
      item.Angle = new Vector2(rand.Randf() * 2.0f - 1.0f, rand.Randf() * 2.0f - 1.0f).Normalized();
      item.LifeTime = item.Velocity * lifeTimeScale_;
      meshes.SetInstanceColor(i, item.Color);
      meshes.SetInstanceTransform2D(i, zero);
    }
  }

  protected override void _Update(ref Item[] items, double integrateTime) {
    integrateTime = 0.3;
    var meshes = Meshes;
    var transform = Transform2D.Identity;
    var nan = new Transform2D().TranslatedLocal(new Vector2(Single.NaN, Single.NaN));
    for (var i = 0; i < MeshCount; ++i) {
      var trans = transform;
      ref var item = ref items[i];
      if (item.LifeTime <= integrateTime) {
        meshes.SetInstanceTransform2D(i, nan);
        continue;
      }
      var v = item.Velocity;
      var lifeTime = item.LifeTime;
      var leftTime = lifeTime - integrateTime;
      var offset = item.Angle * (float)(v * integrateTime - integrateTime * integrateTime * v * 0.2f);
      trans = trans.TranslatedLocal(offset);
      if (leftTime < lifeTime / 2.0f) {
        trans = trans.ScaledLocal(Vector2.One * (float)(leftTime / (lifeTime * 0.5f)));
      }
      meshes.SetInstanceTransform2D(i, trans);
    }
  }
}
