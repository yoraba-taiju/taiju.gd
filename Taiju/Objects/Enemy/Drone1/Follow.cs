using System;
using Godot;
using Taiju.Objects.BulletServer.Server;
using Taiju.Util;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Enemy.Drone1;

public partial class Follow : Base<Follow.Param> {
  [Export] private Vector3 initialVelocity_ = new(-10.0f, 0.0f, 0.0f);
  [Export(PropertyHint.Range, "0,360,")] private float maxRotateDegreePerSec_ = 240.0f;
  [Export(PropertyHint.Range, "0,20,")] private float returnDistance_ = 12.0f;
  [Export(PropertyHint.Range, "0,20,")] private float escapeDistance_ = 13.0f;
  [Export(PropertyHint.Range, "0,30,")] private float bulletSpeed_ = 15.0f;

  //
  public enum State {
    Init,
    Seek,
    Return,
    Escape,
  }

  public struct Param {
    public State State;
    public Vector3 Velocity;
  }

  private int defaultEscapeDirection_;

  public override void _Ready() {
    base._Ready();
    Name = "Drone1/Follow";

    ref var rec = ref Record.Mut;
    ref var param = ref rec.Param;
    
    param.State = State.Init;
    param.Velocity = Vec.Rotate(initialVelocity_, Rotation.Z);

    defaultEscapeDirection_ = ((Random.Shared.Next() % 2) * 2) - 1;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var rec = ref Record.Mut;
    ref var param = ref rec.Param;
    var currentPosition = Position;
    var soraPosition = Sora.Position;
    var maxAngle = (float)(dt * Mathf.DegToRad(maxRotateDegreePerSec_));

    switch (param.State) {
      case State.Init: {
        if (Position.X <= 18.0f) {
          param.State = State.Seek;
        }
      }
        break;

      case State.Seek: {
        var delta = soraPosition - currentPosition;
        if (Mathf.Abs(delta.X) > returnDistance_) {
          param.Velocity = Mover.Follow(delta, param.Velocity, maxAngle);
        } else {
          param.State = State.Return;
          BulletServer.SpawnToSora(rec.Position, bulletSpeed_);
        }
      }
        break;

      case State.Return: {
        var delta = soraPosition - currentPosition;
        var length = delta.Length();
        if (length < returnDistance_) {
          var sign = Mathf.Sign(delta.Y);
          if (sign == 0) {
            sign = defaultEscapeDirection_;
          }
          param.Velocity = Vec.Rotate(param.Velocity, sign * maxAngle) * Mathf.Exp((float)dt / 2);
        } else if (length > escapeDistance_ * 1.1f) {
          BulletServer.SpawnToSora(rec.Position, 15.0f);
          param.State = State.Escape;
        }
      }
        break;

      case State.Escape: {
        param.Velocity = Mover.Follow(Vector3.Right, param.Velocity, maxAngle);
      }
        break;

      default:
        throw new ArgumentOutOfRangeException();
    }

    { // Update godot states
      Body.Rotation = new Vector3(0, 0, Vec.Atan2(-param.Velocity));
    }

    return base._ProcessForward(integrateTime, dt);
  }
  
  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref Record.Ref;
    ref readonly var param = ref rec.Param;
    state.LinearVelocity = param.Velocity;
  }
  
}
