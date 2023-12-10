using System;
using Godot;
using Taiju.Objects.BulletServer.Servers;
using Taiju.Objects.Reversible.Value;
using Taiju.Util.Godot;

namespace Taiju.Objects.Enemy.Drone0;

public partial class Brain : EnemyBase {
  [Export(PropertyHint.Range, "0,180,")] private float maxRotateDegreePerSec_ = 60.0f;
  [Export(PropertyHint.Range, "0,20,")] private float escapeDistance_ = 12.0f;
  private CircleBulletServer circleBulletServer_;

  //
  private enum State {
    Seek,
    Escape,
  }

  private Node3D body_;
  private AnimationPlayer animPlayer_;
  private Dense<Record> record_;
  private RandomNumberGenerator rand_ = new();
  private int defaultEscapeDirection_;

  private record struct Record {
    public State State;
    public Vector3 Position;
    public Vector3 Velocity;
    public double Animation;
    public bool Emitted;
  }

  public override void _Ready() {
    base._Ready();
    body_ = GetNode<Node3D>("Body");
    record_ = new Dense<Record>(Clock, new Record {
      State = State.Seek,
      Position = Position,
      Velocity = new Vector3(-10.0f, 0.0f, 0.0f),
      Animation = 0.0,
      Emitted = false,
    });
    var model = body_.GetNode<Node3D>("Model");
    animPlayer_ = model.GetNode<AnimationPlayer>("AnimationPlayer");
    var anim = animPlayer_.GetAnimation("Rotate");
    anim.LoopMode = Animation.LoopModeEnum.Linear;
    anim.RemoveTrack(anim.GetTrackCount() - 1);
    animPlayer_.PlaybackActive = true;
    animPlayer_.Play("Rotate");
    defaultEscapeDirection_ = ((int)(rand_.Randi() % 2) * 2) - 1;
    circleBulletServer_ = GetNode<CircleBulletServer>("/root/Root/Field/EnemyBullet/CircleBulletServer");
    Shield = 1;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var rec = ref record_.Mut;
    ref var pos = ref rec.Position;
    ref var state = ref rec.State;
    ref var velocity = ref rec.Velocity;
    var currentPosition = rec.Position;
    var soraPosition = Sora.Position;
    var maxAngle = (float)(dt * maxRotateDegreePerSec_);

    switch (state) {
      case State.Seek: {
        var delta = soraPosition - currentPosition;
        if (Mathf.Abs(delta.X) > escapeDistance_) {
          velocity = Mover.Follow(delta, rec.Velocity, maxAngle);
        }
        else {
          state = State.Escape;
          circleBulletServer_.SpawnToSora(pos, 15.0f);
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

          if (delta.Length() > escapeDistance_ * 2.0f) {
            // shot once
            if (!rec.Emitted) {
              rec.Emitted = true;
            }
          }

          velocity = Vec.Rotate(rec.Velocity, sign * maxAngle) * Mathf.Exp((float)dt / 2);
        }
      }
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }

    // Update or Record godot states
    pos = Position;
    body_.Rotation = new Vector3(0, 0, Mathf.DegToRad(Vec.Atan2(-velocity)));
    rec.Animation = animPlayer_.CurrentAnimationPosition;

    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    return LoadCurrentStatus();
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref record_.Ref;
    state.LinearVelocity = rec.Velocity;
  }

  private bool LoadCurrentStatus() {
    ref readonly var rec = ref record_.Ref;
    Position = rec.Position;
    body_.Rotation = new Vector3(0, 0, Mathf.DegToRad(Vec.Atan2(-rec.Velocity)));
    animPlayer_.Seek(rec.Animation, true);
    return true;
  }
}
