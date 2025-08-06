using System;
using Godot;
using Taiju.Objects.BulletServer.Server;
using Taiju.Util;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Enemy.Drone1;

public partial class FollowCurve : Base<FollowCurve.Param>, IEnemyWithCurve {
  [Export(PropertyHint.Range, "0,20,")] private float speed_ = 10.0f;
  public Curve3D Curve { protected get; set; }

  public struct Param {
  }

  public override void _Ready() {
    base._Ready();
    Name = "Drone1/FollowCurve";

    ref var rec = ref Record.Mut;
    rec.Position = Curve.SampleBaked();
    ref var param = ref rec.Param;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var rec = ref Record.Mut;
    ref var param = ref rec.Param;
    var currentPosition = Position;

    Position = Curve.SampleBaked((float)(integrateTime * speed_));

    { // Update godot states
      Body.Rotation = new Vector3(0, 0, Vec.Atan2(-(Position - currentPosition)));
    }

    return base._ProcessForward(integrateTime, dt);
  }
}
