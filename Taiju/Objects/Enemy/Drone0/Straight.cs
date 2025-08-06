using Godot;
using Taiju.Objects.BulletServer.Server;
using Taiju.Util;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Enemy.Drone0;

public partial class Straight : Base<Straight.Param> {
  [Export] private Vector3 initialVelocity_ = new(-10.0f, 0.0f, 0.0f);
  [Export(PropertyHint.Flags)] private bool shotToSora_;
  [Export(PropertyHint.Range, "0,30,")] private float bulletSpeed_ = 15.0f;
  private Vector3 rotatedInitialVelocity_;

  public record struct Param {
    public bool ShotToSora;
    public Vector3 Velocity;
  }

  public override void _Ready() {
    base._Ready();
    Name = "Drone0/Straight";
    ref var param = ref Record.Mut.Param;
    param = new Param {
      ShotToSora = shotToSora_,
      Velocity = initialVelocity_,
    };
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var param = ref Record.Mut.Param;

    if (Position.X <= 18.0f && param.ShotToSora) {
      param.ShotToSora = false;
      BulletServer.SpawnToSora(Position, bulletSpeed_);
    }

    { // Update godot states
      Body.Rotation = new Vector3(0, 0, Vec.Atan2(-param.Velocity));
    }

    return base._ProcessForward(integrateTime, dt);
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref Record.Ref;
    state.LinearVelocity = rec.Param.Velocity;
  }
}
