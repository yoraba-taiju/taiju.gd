using Godot;
using Taiju.Objects.Reversible.Godot;

namespace Taiju.Objects.Effect;

public partial class StarDust : ReversibleParticle3D<StarDust.Param> {
  [Export] protected Color BaseColor = Colors.White;
  [Export(PropertyHint.Range, "0,360,")] protected float Angle;
  [Export(PropertyHint.Range, "0,180,")] protected float Range = 45.0f;
  [Export] protected double DecayRate = 0.9;

  public struct Param {
    public Transform2D EmitTransform;
    public Color Color;
    public float Velocity;
    public Vector2 Direction;
    public double LifeTime;
  }

  private Transform2D CalcTransform2D() {
    return new Transform2D(
      GlobalTransform.Basis.X.X, GlobalTransform.Basis.X.Y,
      GlobalTransform.Basis.Y.X, GlobalTransform.Basis.Y.Y,
      GlobalTransform.Origin.X, GlobalTransform.Origin.Y);
  }

  private RandomNumberGenerator rand_ = new();
  protected override void _EmitOne(ref Param param) {
    param.EmitTransform = CalcTransform2D();
    BaseColor.ToHsv(out _, out var saturation, out var value);
    param.Color = Color.FromHsv(rand_.Randf() * 360.0f, saturation, value);
    param.Velocity = MaxSpeed * (rand_.Randf() / 2.0f + 0.5f);
    var angle = Mathf.DegToRad(Angle - Range / 2 + rand_.Randf() * Range);
    param.Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    param.LifeTime = 1.0;
  }

  protected override bool _Update(ref readonly Param param, double t) {
    return t <= param.LifeTime;
  }

  protected override void _SetInstance(int i, ref readonly Param param, double t) {
    var rate = t / param.LifeTime;
    var alpha = 1.0f - (float)rate;

    var color = param.Color;
    color.A *= alpha;
    Meshes.SetInstanceColor(i, color);
    var trans = (CalcTransform2D().Inverse() * param.EmitTransform).TranslatedLocal(param.Direction * param.Velocity * (float)t);
    Meshes.SetInstanceTransform2D(i, trans);
  }
}
