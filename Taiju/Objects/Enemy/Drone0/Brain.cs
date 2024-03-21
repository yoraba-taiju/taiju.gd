using System;
using Godot;
using Taiju.Objects.Reversible.Value;
using Taiju.Util.Godot;

namespace Taiju.Objects.Enemy.Drone0;

public partial class Brain : EnemyBase {
  [Export(PropertyHint.Range, "0,100,1")] private int initialShield_ = 4;
  [Export] private Vector3 initialVelocity_ = new(-10.0f, 0.0f, 0.0f);
  [Export(PropertyHint.Range, "0,180,")] private float maxRotateDegreePerSec_ = 60.0f;
  [Export(PropertyHint.Range, "0,20,")] private float escapeDistance_ = 12.0f;

  //
  private enum State {
    Init,
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
    public int Shield;
    public Vector3 Position;
    public Vector3 Velocity;
    public double Animation;
  }

  public override void _Ready() {
    base._Ready();
    body_ = GetNode<Node3D>("Body")!;
    record_ = new Dense<Record>(Clock, new Record {
      State = State.Init,
      Shield = initialShield_,
      Position = Position,
      Velocity = initialVelocity_,
      Animation = 0.0,
    });
    var model = body_.GetNode<Node3D>("Model")!;
    animPlayer_ = model.GetNode<AnimationPlayer>("AnimationPlayer")!;
    var anim = animPlayer_.GetAnimation("Rotate")!;
    anim.LoopMode = Animation.LoopModeEnum.Linear;
    animPlayer_.PlaybackActive = true;
    animPlayer_.Play("Rotate");
    defaultEscapeDirection_ = ((int)(rand_.Randi() % 2) * 2) - 1;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    base._ProcessForward(integrateTime, dt);
    ref var rec = ref record_.Mut;
    { // Record godot states
      rec.Position = Position;
      rec.Animation = animPlayer_.CurrentAnimationPosition;
    }
    var currentPosition = rec.Position;
    var soraPosition = Sora.Position;
    var maxAngle = (float)(dt * Mathf.DegToRad(maxRotateDegreePerSec_));

    switch (rec.State) {
      case State.Init: {
        rec.Velocity = initialVelocity_;
        if (Position.X <= 18.0f) {
          rec.State = State.Seek;
        }
      }
        break;

      case State.Seek: {
        var delta = soraPosition - currentPosition;
        if (Mathf.Abs(delta.X) > escapeDistance_) {
          rec.Velocity = Mover.Follow(delta, rec.Velocity, maxAngle);
        }
        else {
          rec.State = State.Escape;
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
          rec.Velocity = Vec.Rotate(rec.Velocity, sign * maxAngle) * Mathf.Exp((float)dt / 2);
        }
      }
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
    body_.Rotation = new Vector3(0, 0, Vec.Atan2(-rec.Velocity));

    return true;
  }
  
  public override bool _ProcessBack(double integrateTime) {
    return LoadCurrentStatus();
  }


  private bool LoadCurrentStatus() {
    ref readonly var rec = ref record_.Ref;
    Position = rec.Position;
    body_.Rotation = new Vector3(0, 0, Vec.Atan2(-rec.Velocity));
    animPlayer_.Seek(rec.Animation, true);
    return true;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref record_.Ref;
    state.LinearVelocity = rec.Velocity;
  }
  protected override ref int ShieldMut => ref record_.Mut.Shield;
}
