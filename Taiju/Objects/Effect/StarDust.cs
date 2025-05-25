using Godot;
using Taiju.Util.Reversible.Godot;

namespace Taiju.Objects.Effect;

public partial class StarDust : ReversibleParticle3D<StarDust.Param> {
  [Export] private Color baseColor_ = Colors.White;
  [Export(PropertyHint.Range, "0,360,")] private float emitAngle_;
  [Export(PropertyHint.Range, "0,180,")] private float emitRange_ = 45.0f;
  [Export] private double decayRate_ = 0.9;

  public struct Param {
    public Transform2D EmitTransform;
    public Color Color;
    public float Velocity;
    public Vector2 Direction;
  }

  private Transform2D CalcTransform2D() {
    var trans = GlobalTransform;
    return new Transform2D(
      trans.Basis.X.X, trans.Basis.X.Y,
      trans.Basis.Y.X, trans.Basis.Y.Y,
      trans.Origin.X, trans.Origin.Y);
  }

  private RandomNumberGenerator rand_ = new();
  protected override double _Emit(int i, ref Param param) {
    param.EmitTransform = CalcTransform2D();
    baseColor_.ToHsv(out _, out var saturation, out var value);
    param.Color = Color.FromHsv(rand_.Randf() * 360.0f, saturation, value);
    param.Velocity = MaxSpeed * (rand_.Randf() / 2.0f + 0.5f);
    var angle = Mathf.DegToRad(emitAngle_ + rand_.RandfRange(-emitRange_/2.0f, emitRange_ / 2.0f));
    param.Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    return 1.0;
  }

  protected override void _UpdateItem(int i, ref readonly Param param, double lifeTime, double t) {
    var scale = Mathf.Max(0.0f, (float)((lifeTime - t) / lifeTime));

    var color = param.Color;
    color.A *= scale;
    var trans = (CalcTransform2D().Inverse() * param.EmitTransform)
      .TranslatedLocal(param.Direction * param.Velocity * (float)t)
      .ScaledLocal(Vector2.One * scale);

    Meshes.SetInstanceColor(i, color);
    Meshes.SetInstanceTransform2D(i, trans);
  }
}
