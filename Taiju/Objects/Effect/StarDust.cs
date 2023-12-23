using Godot;
using Taiju.Objects.Reversible.Godot;

namespace Taiju.Objects.Effect;

public partial class StarDust : ReversibleParticle3D {
  [Export] protected Color BaseColor = Colors.White;
  [Export(PropertyHint.Range, "0,360,")] protected float Angle;
  [Export(PropertyHint.Range, "0,180,")] protected float Range = 45;
  [Export] protected double DecayRate = 0.9;

  private RandomNumberGenerator rand_ = new();
  protected override void _EmitOne(ref Item item, double integrateTime) {
    BaseColor.ToHsv(out _, out var saturation, out var value);
    item.Color = Color.FromHsv(rand_.Randf() * 360.0f, saturation, value);
    item.Velocity = MaxSpeed * (rand_.Randf() / 2.0f + 0.5f);
    var angle = Mathf.DegToRad(Angle - Range / 2 + rand_.Randf() * Range);
    item.Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    item.LifeTime = 1.0;
  }

  protected override bool _Update(ref readonly Item item, double integrateTime) {
    var dt = integrateTime - item.EmitAt;
    return dt <= item.LifeTime;
  }

  protected override void _SetInstance(int i, ref readonly Item item, double integrateTime) {
    var t = (float)(integrateTime - item.EmitAt);
    var rate = t / item.LifeTime;
    var alpha = 1.0f - (float)(rate);

    var color = item.Color;
    color.A *= alpha;
    Meshes.SetInstanceColor(i, color);
    var trans = Transform2D.Identity.Translated(item.Direction * item.Velocity * t);
    Meshes.SetInstanceTransform2D(i, trans);
  }
}
