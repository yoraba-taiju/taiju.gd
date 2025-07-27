using System;
using Godot;
using Taiju.Util;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Enemy.Drone0;

public partial class Escape : Base<Escape.Param> {
  [Export] private Vector3 initialVelocity_ = new(-10.0f, 0.0f, 0.0f);
  [Export(PropertyHint.Range, "0,180,")] private float maxRotateDegreePerSec_ = 60.0f;
  [Export(PropertyHint.Range, "0,20,")] private float escapeDistance_ = 12.0f;

  //
  public enum State {
    Init,
    Seek,
    Escape,
  }

  private RandomNumberGenerator rand_ = new();
  private int defaultEscapeDirection_;

  public record struct Param {
    public State State;
  }

  public override void _Ready() {
    base._Ready();
    Name = "Drone0/Escape";
    ref var param = ref Record.Mut.Param;
    param.State = State.Init;
    defaultEscapeDirection_ = ((int)(rand_.Randi() % 2) * 2) - 1;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var rec = ref Record.Mut;
    ref var param = ref rec.Param;

    var currentPosition = rec.Position;
    var soraPosition = Sora.Position;
    var maxAngle = (float)(dt * Mathf.DegToRad(maxRotateDegreePerSec_));

    switch (param.State) {
      case State.Init: {
        rec.Velocity = initialVelocity_;
        if (Position.X <= 18.0f) {
          param.State = State.Seek;
        }
      }
        break;

      case State.Seek: {
        var delta = soraPosition - currentPosition;
        if (Mathf.Abs(delta.X) > escapeDistance_) {
          Velocity = Mover.Follow(delta, rec.Velocity, maxAngle);
        }
        else {
          param.State = State.Seek;
        }
      }
        break;

      case State.Escape: {
        var delta = soraPosition - currentPosition;
        if (delta.Length() < escapeDistance_) {
          var sign = Mathf.Sign(delta.Y);
          if (sign == 0) {
            sign = defaultEscapeDirection_;
          }
          Velocity = Vec.Rotate(rec.Velocity, sign * maxAngle) * Mathf.Exp((float)dt / 2);
        }
      }
        break;

      default:
        throw new ArgumentOutOfRangeException();
    }

    return base._ProcessForward(integrateTime, dt);
  }
}
