using Godot;
using Taiju.Util.Reversible.Godot;

namespace Taiju.Objects.Effect;

public partial class ReversibleExplosion : ReversibleOneShotParticle3D<ReversibleExplosion.Param> {
  [Export] private float maxSpeed_ = 10.0f;
  [Export] private Color color_ = Colors.Purple;
  [Export] private bool replaceHueWithRandomAngle_;
  [Export] private double lifeTimeScale_ = 1.0 / 20.0;
  private RandomNumberGenerator rand_ = new();

  public struct Param {
    public Color Color;
    public float Speed;
    public Vector2 Direction;
  }

  protected override double _Emit(int i, ref Param param) {
    if (replaceHueWithRandomAngle_) {
      color_.ToHsv(out _, out var saturation, out var value);
      param.Color = Color.FromHsv(rand_.Randf() * 360.0f, saturation, value);
    } else {
      param.Color = color_;
    }
    param.Speed = maxSpeed_ * rand_.RandfRange(0.5f, 1.0f);
    param.Direction = new Vector2(rand_.Randf() * 2.0f - 1.0f, rand_.Randf() * 2.0f - 1.0f).Normalized();
    return param.Speed * lifeTimeScale_;
  }

  protected override void _UpdateItem(int i, ref readonly Param param, double lifeTime, double t) {
    var speed = param.Speed;
    var offset = param.Direction * (float)(speed * t);
    var leftTime = lifeTime - t;
    var trans = Transform2D.Identity.TranslatedLocal(offset);
    if (leftTime < lifeTime / 2.0f) {
      var scale = (float)(leftTime / (lifeTime * 0.5));
      trans = trans.ScaledLocal(Vector2.One * scale);
    }
    Meshes.SetInstanceTransform2D(i, trans);
    Meshes.SetInstanceColor(i, param.Color);
  }
}
