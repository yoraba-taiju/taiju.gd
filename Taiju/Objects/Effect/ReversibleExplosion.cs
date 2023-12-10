using System;
using Godot;
using Taiju.Objects.Reversible.Godot;

namespace Taiju.Objects.Effect;

public partial class ReversibleExplosion : ReversibleOneShotParticle3D {
  [Export] private Color color_ = Colors.Purple;
  [Export] private bool replaceHueWithRandomAngle_;
  private const double LifeTimeScale = 1.0 / 20.0;
  private static readonly Transform2D TransNaN = new Transform2D().TranslatedLocal(new Vector2(Single.NaN, Single.NaN));

  protected override void _SetInstance(int i, ref readonly Item item, double integrateTime) {
    var trans = Transform2D.Identity;
    if (item.LifeTime <= integrateTime) {
      Meshes.SetInstanceTransform2D(i, TransNaN);
      return;
    }
    var v = item.Velocity;
    var lifeTime = item.LifeTime;
    var leftTime = lifeTime - integrateTime;
    var offset = item.Angle * (float)(v * integrateTime - integrateTime * integrateTime * v * 0.2f);
    trans = trans.TranslatedLocal(offset);
    if (leftTime < lifeTime / 2.0f) {
      trans = trans.ScaledLocal(Vector2.One * (float)(leftTime / (lifeTime * 0.5f)));
    }
    Meshes.SetInstanceTransform2D(i, trans);
  }

  protected override double _Lifetime() {
    return MaxSpeed * LifeTimeScale;
  }

  protected override void _Emit(ref Item[] items) {
    var rand = new RandomNumberGenerator();
    var zero = Transform2D.Identity;
    var maxSpeed = MaxSpeed;
    var meshes = Meshes;
    color_.ToHsv(out _, out var saturation, out var value);
    for (var i = 0; i < MeshCount; ++i) {
      ref var item = ref items[i];
      item.Color = replaceHueWithRandomAngle_ ? Color.FromHsv(rand.Randf() * 360.0f, saturation, value) : color_;
      item.Velocity = maxSpeed * (rand.Randf() / 2.0f + 0.5f);
      item.Angle = new Vector2(rand.Randf() * 2.0f - 1.0f, rand.Randf() * 2.0f - 1.0f).Normalized();
      item.LifeTime = item.Velocity * LifeTimeScale;
      meshes.SetInstanceColor(i, item.Color);
      meshes.SetInstanceTransform2D(i, zero);
    }
  }
}
