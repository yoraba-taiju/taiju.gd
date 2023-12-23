using Godot;
using Taiju.Objects.Reversible.Godot;

namespace Taiju.Objects.Effect;

public partial class StarDust : ReversibleParticle3D {
  [Export] protected Color BaseColor = Colors.White;
  [Export] protected double DecayRate = 0.9;

  public override void _Ready() {
  }

  private RandomNumberGenerator rand_ = new RandomNumberGenerator();
  protected override void _EmitOne(ref Item item, double integrateTime) {
    BaseColor.ToHsv(out _, out var saturation, out var value);
    item.Color = Color.FromHsv(rand_.Randf() * 360.0f, saturation, value);
    item.Velocity = MaxSpeed * (rand_.Randf() / 2.0f + 0.5f);
    item.Angle = new Vector2(rand_.Randf() * 2.0f - 1.0f, rand_.Randf() * 2.0f - 1.0f).Normalized();
    item.LifeTime = Mathf.Log(0.1 / item.Velocity) / Mathf.Log(DecayRate);
  }

  protected override bool _Update(ref Item item, double
    integrateTime) {
    var t = integrateTime - item.EmitAt;
    return t <= item.LifeTime;
  }

  protected override void _SetInstance(int i, ref readonly Item item, double integrateTime) {
    var t = integrateTime - item.EmitAt;
    var rate = t / item.LifeTime;
    var alpha = (float)Mathf.Sqrt(rate);
    var color = item.Color;
    color.A *= alpha;
    Meshes.SetInstanceColor(i, color);
  }
}
