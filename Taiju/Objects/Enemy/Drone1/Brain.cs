using System;
using Godot;
using Taiju.Objects.BulletServer.Servers;
using Taiju.Objects.Reversible.Value;
using Taiju.Util.Godot;

namespace Taiju.Objects.Enemy.Drone1;

public partial class Brain : EnemyBase {
  [Export(PropertyHint.Range, "0,360,")] private float maxRotateDegreePerSec_ = 180.0f;
  [Export(PropertyHint.Range, "0,20,")] private float returnDistance_ = 12.0f;
  private const string SeekReq = "parameters/Seek/seek_request";
  private CircleBulletServer circleBulletServer_;

  //
  private enum State {
    Seek,
    Return,
    Escape,
  }

  private Node3D body_;
  private AnimationTree animationTree_;
  private Dense<Record> record_;
  private RandomNumberGenerator rand_ = new();
  private int defaultEscapeDirection_;

  private record struct Record {
    public int Shield;
    public State State;
    public Vector3 Position;
    public Vector3 Velocity;
  }

  public override void _Ready() {
    base._Ready();
    body_ = GetNode<Node3D>("Body");

    animationTree_ = GetNode<AnimationTree>("AnimationTree");
    animationTree_.Active = true;

    record_ = new Dense<Record>(Clock, new Record {
      Shield = 4,
      State = State.Seek,
      Position = Position,
      Velocity = new Vector3(-10.0f, 0.0f, 0.0f),
    });

    defaultEscapeDirection_ = ((int)(rand_.Randi() % 2) * 2) - 1;
    circleBulletServer_ = GetNode<CircleBulletServer>("/root/Root/Field/EnemyBullet/CircleBulletServer");
    animationTree_.Set(SeekReq, 0f);
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    base._ProcessForward(integrateTime, dt);
    ref var rec = ref record_.Mut;
    { // Record godot states
      rec.Position = Position;
    }
    var currentPosition = rec.Position;
    var soraPosition = Sora.Position;
    var maxAngle = (float)(dt * maxRotateDegreePerSec_);

    switch (rec.State) {
      case State.Seek: {
        var delta = soraPosition - currentPosition;
        if (Mathf.Abs(delta.X) > returnDistance_) {
          rec.Velocity = Mover.Follow(delta, rec.Velocity, maxAngle);
        } else {
          rec.State = State.Return;
          circleBulletServer_.SpawnToSora(rec.Position, 15.0f);
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
          rec.Velocity = Vec.Rotate(rec.Velocity, sign * maxAngle) * Mathf.Exp((float)dt / 2);
        } else if (length > returnDistance_ * 1.1f) {
          circleBulletServer_.SpawnToSora(rec.Position, 15.0f);
          rec.State = State.Escape;
        }
      }
        break;

      case State.Escape: {
        rec.Velocity = Mover.Follow(Vector3.Right, rec.Velocity, maxAngle);
      }
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }

    { // Update godot states
      body_.Rotation = new Vector3(0, 0, Mathf.DegToRad(Vec.Atan2(-rec.Velocity)));
    }

    return true;
  }

  public override void Hit() {
    if (!IsAlive) {
      return;
    }
    ref var rec = ref record_.Mut;
    rec.Shield -= 1;
    if (rec.Shield > 0) {
      return;
    }
    base.Destroy();
  }

  public override bool _ProcessBack(double integrateTime) {
    return LoadCurrentStatus(integrateTime);
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref record_.Ref;
    state.LinearVelocity = rec.Velocity;
  }

  private bool LoadCurrentStatus(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    Position = rec.Position;
    body_.Rotation = new Vector3(0, 0, Mathf.DegToRad(Vec.Atan2(-rec.Velocity)));
    animationTree_.Set(SeekReq, integrateTime);
    return true;
  }
}
