using Godot;
using Taiju.Objects.Witch;
using Taiju.Util;
using Taiju.Util.Reversible.Godot;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Effect;

public partial class MagicElementItem : ReversibleNode3D {
  [Export] private Color baseColor_ = Colors.White;
  [Export(PropertyHint.Range, "0,128.0,1.0")] private float defaultSpeedMin_ = 18.0f;
  [Export(PropertyHint.Range, "0,128.0,1.0")] private float defaultSpeedMax_ = 24.0f;
  [Export(PropertyHint.Range, "0,128.0,1.0")] private float initialFriction_ = 40.0f;
  [Export(PropertyHint.Range, "0,5.0,0.1")] private float leftPeriodMin_ = 0.9f;
  [Export(PropertyHint.Range, "0,5.0,0.1")] private float leftPeriodMax_ = 1.1f;

  public struct Record {
    public double LeftPeriod;
    public Vector3 Position;
    public Vector3 Velocity;
  }

  private Sora sora_;
  private Color color_;
  private Dense<Record> record_;
  private RandomNumberGenerator rand_ = new();

  // Nodes
  private Sprite3D sprite_;

  public override void _Ready() {
    base._Ready();
    sora_ = GetNode<Sora>("/root/Root/Field/Witch/Sora")!;
    sprite_ = GetNode<Sprite3D>("Sprite")!;
    { // Color
      baseColor_.ToHsv(out _, out var saturation, out var value);
      color_ = Color.FromHsv(rand_.Randf() * 360.0f, saturation, value);
      sprite_.Modulate = color_;
    }
    var velocity =
      new Vector3(rand_.RandfRange(-1.0f, 1.0f), rand_.RandfRange(-1.0f, 1.0f), 0.0f).Normalized() *
      rand_.RandfRange(defaultSpeedMin_, defaultSpeedMax_);
    record_ = new Dense<Record>(Clock, new Record {
      LeftPeriod = rand_.RandfRange(leftPeriodMin_, leftPeriodMax_),
      Position = Position,
      Velocity = velocity,
    });
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    var soraPos = sora_.Position;
    ref var rec = ref record_.Mut;
    ref var pos = ref rec.Position;
    ref var vel = ref rec.Velocity;
    ref var leftPeriod = ref rec.LeftPeriod;
    // Move element.
    if (leftPeriod >= 0.5) {
      var force = vel.Normalized() * -initialFriction_;
      vel += force * (float)dt;
    } else {
      var force = Mover.TrackingForce(pos, vel, soraPos, Vector3.Zero, leftPeriod);
      vel += force * (float)dt;
    }
    pos += vel * (float)dt;
    leftPeriod -= dt;
    if (leftPeriod <= 0) {
      sora_.AbsorbMagicElement();
      Destroy();
    }

    { // Update status by record.
      Position = pos;
    }
    SetBillboard();
    return true;
  }
  public override bool _ProcessBack(double integrateTime) {
    LoadCurrentStatus();
    SetBillboard();
    return true;
  }

  private void LoadCurrentStatus() {
    ref readonly var rec = ref record_.Ref;
    {
      Position = rec.Position;
    }
  }

  private void SetBillboard() {
    // https://github.com/godotengine/godot-proposals/discussions/5821
    var camera = GetViewport().GetCamera3D();
    var lookPos = camera.GlobalTransform.Origin;
    lookPos.Y = GlobalTransform.Origin.Y;
    LookAt(lookPos, camera.GlobalTransform.Basis.Y);
  }
}
