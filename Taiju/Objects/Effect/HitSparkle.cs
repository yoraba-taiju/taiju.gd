using Godot;
using Taiju.Util.Reversible.Godot;

namespace Taiju.Objects.Effect;

public partial class HitSparkle : ReversibleOneShotParticle3D<HitSparkle.Param> {
  [Export(PropertyHint.ColorNoAlpha)] private Color color_ = Color.FromHtml("#5371ff");
  [Export] private float maxSpeed_= 20.0f;
  [Export(PropertyHint.Range, "0,360,")] private float emitAngle_ = 180.0f;
  [Export(PropertyHint.Range, "0,180,")] private float emitRange_ = 90.0f;
  [Export] private double lifeTimeScale_ = 40.0;
  private RandomNumberGenerator rand_ = new();

  public struct Param {
    public float Speed;
    public Vector2 Direction;
  }

  protected override double _Emit(int i, ref Param param) {
    param.Speed = rand_.RandfRange(0.0f, maxSpeed_);
    var angle = Mathf.DegToRad(emitAngle_ + rand_.RandfRange(-emitRange_/2.0f, emitRange_ / 2.0f));
    param.Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    return param.Speed / lifeTimeScale_;
  }

  protected override void _UpdateItem(int i, ref readonly Param param, double lifeTime, double t) {
    var scale = Mathf.Max(0.0f, (float)((lifeTime - t) / lifeTime));

    var color = color_;
    color.A *= scale;

    var speed = param.Speed;
    var offset = param.Direction * (float)(speed * t);
    var trans =
      Transform2D.Identity
        .TranslatedLocal(offset)
        .ScaledLocal(Vector2.One * scale);

    Meshes.SetInstanceColor(i, color);
    Meshes.SetInstanceTransform2D(i, trans);
  }
}
