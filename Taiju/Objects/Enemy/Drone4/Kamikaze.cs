using System;
using Godot;
using Taiju.Objects.Witch;
using Taiju.Util;

namespace Taiju.Objects.Enemy.Drone4;

public partial class Kamikaze : Base<Kamikaze.Param> {
  [Export] private float initialSpeed_ = 10.0f;
  [Export] private float kamikazeSpeed_ = 40.0f;
  [Export(PropertyHint.Range, "0,10,")] private float initialStarDustSpeed_ = 5.0f;
  [Export(PropertyHint.Range, "0,30,")] private float kamikazeStarDustSpeed_ = 25.0f;
  [Export(PropertyHint.Range, "0,360,")] private float maxRotateDegreePerSec_ = 60.0f;
  [Export(PropertyHint.Range, "0,20,")] private float prepareDistance_ = 15.0f;
  [Export(PropertyHint.Range, "0,20,")] private double timeOfPrepare_ = 0.5;
  [Export(PropertyHint.Range, "0,20,")] private double timeOfPreKamikaze_ = 0.3;

  // Param
  public enum State {
    Init,
    Prepare,
    PreKamikaze,
    Kamikaze,
  }
  public struct Param {
    public State State;
    public double TimeToNext;
  }

  // Nodes

  public override void _Ready() {
    base._Ready();
    ref var rec = ref Record.Mut;
    rec.Speed = initialSpeed_;
    rec.Direction = Sora.Position - Position; // overwrite
    ref var param = ref rec.Param;
    param = new Param {
      State = State.Init,
      TimeToNext = 0.0,
    };
    StarDust.MaxSpeed = initialStarDustSpeed_;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var rec = ref Record.Mut;
    ref var param = ref rec.Param;
    var targetDirection = Sora.Position - Position;
    switch (param.State) {
      case State.Init: {
        rec.Direction = Mover.Follow(targetDirection, rec.Direction, maxRotateDegreePerSec_);
        var distance = targetDirection.Length();
        if (distance <= prepareDistance_) {
          param.State = State.Prepare;
          param.TimeToNext = timeOfPrepare_;

          StarDust.Visible = false;
          rec.Speed = 0.0f;
        }
      }
        break;
      case State.Prepare:
        rec.Direction = targetDirection;
        param.TimeToNext -= dt;
        if (param.TimeToNext < 0.0) {
          param.State = State.PreKamikaze;
          param.TimeToNext = timeOfPreKamikaze_;

          StarDust.Visible = true;
          StarDust.MaxSpeed = kamikazeStarDustSpeed_;
          rec.Speed = 0.0f;
        }
        break;
      case State.PreKamikaze:
        param.TimeToNext -= dt;
        if (param.TimeToNext < 0.0) {
          param.State = State.Kamikaze;

          StarDust.Visible = true;
          StarDust.MaxSpeed = kamikazeStarDustSpeed_;
          rec.Speed = kamikazeSpeed_;
        }
        break;
      case State.Kamikaze:
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }

    return base._ProcessForward(integrateTime, dt);
  }

  public override bool _ProcessBack(double integrateTime) {
    LoadCurrentStatus(integrateTime);
    return base._ProcessBack(integrateTime);
  }

  private void LoadCurrentStatus(double integrateTime) {
    ref readonly var rec = ref Record.Ref;
    ref readonly var param = ref rec.Param;
    switch (param.State) {
      case State.Init:
        StarDust.Visible = true;
        StarDust.MaxSpeed = initialStarDustSpeed_;
        break;
      case State.Prepare:
        StarDust.Visible = false;
        StarDust.MaxSpeed = 0.0f;
        break;
      case State.PreKamikaze:
        StarDust.Visible = true;
        break;
      case State.Kamikaze:
        StarDust.Visible = true;
        StarDust.MaxSpeed = kamikazeStarDustSpeed_;
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }
}
