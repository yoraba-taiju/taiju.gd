using System;
using Godot;
using Taiju.Objects.BulletServer.Server;
using Taiju.Objects.Effect;
using Taiju.Util;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Enemy.Drone3;
// https://code.ledyba.org/yoraba-taiju/taiju.unity/src/branch/magistra/Assets/Scripts/Enemy/Drone/Drone3.cs

public partial class Straight : EnemyBase {
  [Export] private Vector3 initialVelocity_ = new(-10.0f, 0.0f, 0.0f);
  private Vector3 rotatedInitialVelocity_;
  [Export(PropertyHint.Range, "0,20,")] private float seekSpeed_ = 7.0f;
  [Export(PropertyHint.Range, "0,40,")] private float escapeSpeed_ = 23.0f;
  [Export(PropertyHint.Range, "0,20")] private double durationToFire_ = 10.0;
  [Export(PropertyHint.Range, "0,20")] private float timeToFire_ = 0.5f;
  [Export(PropertyHint.Range, "0,20")] private float rotateToFire_ = 30.0f;
  [Export(PropertyHint.Range, "0,30,")] private float bulletSpeed_ = 15.0f;

  //
  private enum State {
    Init,
    Fire,
    Prepare,
    Escape,
  }

  private Node3D body_;
  private StarDust starDust_;
  private AnimationPlayer animPlayer_;
  private CircleBulletServer bulletServer_;
  private Dense<Record> record_;

  private record struct Record {
    public State State;
    public int Shield;
    public Vector3 Position;
    public Vector3 Velocity;
    public double TimeToFire;
    public double NextTimeToAction;
    public double Animation;
  }

  public override void _Ready() {
    base._Ready();
    Name = "Drone3/Straight";
    body_ = GetNode<Node3D>("Body")!;
    
    // Body size
    BodySize = new Vector2(1.25f, 1.5f);

    rotatedInitialVelocity_ = Vec.Rotate(initialVelocity_, Rotation.Z);
    Rotation = new Vector3(Rotation.X, Rotation.Y, 0.0f);

    // Animation
    var model = body_.GetNode<Node3D>("Drone3")!;
    animPlayer_ = model.GetNode<AnimationPlayer>("AnimationPlayer")!;
    var anim = animPlayer_.GetAnimation("BarretRoll")!;
    anim.LoopMode = Animation.LoopModeEnum.Linear;
    animPlayer_.PlaybackActive = true;
    animPlayer_.Play("BarretRoll");

    // Star dust
    starDust_ = body_.GetNode<StarDust>("StarDust")!;
    starDust_.Visible = false;

    // Bullet server
    bulletServer_ = GetNode<CircleBulletServer>("/root/Root/Field/EnemyBullet/BlueCircleBulletServer")!;
    record_ = new Dense<Record>(Clock, new Record {
      State = State.Init,
      Shield = InitialShield,
      Position = Position,
      Velocity = rotatedInitialVelocity_,
      TimeToFire = timeToFire_,
      NextTimeToAction = 0.0f,
    });
  }
  
  public override bool _ProcessForward(double integrateTime, double dt) {
    base._ProcessForward(integrateTime, dt);
    ref var rec = ref record_.Mut;
    { // Record godot states
      rec.Position = Position;
    }
    var currentPosition = rec.Position;
    var currentVelocity = rec.Velocity;
    ref var nextTimeToAction = ref rec.NextTimeToAction;
    ref var state = ref rec.State;

    switch (state) {
      case State.Init: {
        rec.Velocity = rotatedInitialVelocity_;
        if (Position.X <= 23.0f) {
          state = State.Fire;
          nextTimeToAction = integrateTime + durationToFire_;
        }
      }
        break;

      case State.Fire: {
        currentVelocity *= Mathf.Exp((float)-dt);
        ref var timeToFire = ref rec.TimeToFire;
        timeToFire -= dt;
        if (timeToFire < 0.0) {
          var barrelPosition = new Vector3(-2.0f, 0f, 0f);
          var velocity2d = new Vector2(-bulletSpeed_, 0f);
          bulletServer_.Spawn(currentPosition + barrelPosition, velocity2d);
          velocity2d = new Vector2(-bulletSpeed_, 0f).Rotated(Mathf.DegToRad(rotateToFire_));
          bulletServer_.Spawn(currentPosition + barrelPosition, velocity2d);
          velocity2d = new Vector2(-bulletSpeed_, 0f).Rotated(Mathf.DegToRad(-rotateToFire_));
          bulletServer_.Spawn(currentPosition + barrelPosition, velocity2d);
          // Next
          timeToFire += timeToFire_;
        }

        if (nextTimeToAction <= integrateTime) {
          state = State.Prepare;
          starDust_.Visible = true;
          nextTimeToAction = integrateTime + 1.0;
        }
      }
        break;

      case State.Prepare: {
        var leftTime = (float)(nextTimeToAction - integrateTime);
        currentVelocity = initialVelocity_ * Mathf.Exp(1.0f - leftTime) / Mathf.E;
        if (leftTime <= 0) {
          state = State.Escape;
        }
      }
        break;

      case State.Escape: {
        currentVelocity *= Mathf.Exp((float)dt);
      }
        break;

      default:
        throw new ArgumentOutOfRangeException();
    }
    
    { // Record state
      rec.Velocity = currentVelocity;
      rec.Animation = animPlayer_.CurrentAnimationPosition;
    }
    { // Update godot states
    }

    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    base._ProcessBack(integrateTime);
    return LoadCurrentStatus(integrateTime);
  }

  private bool LoadCurrentStatus(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    Position = rec.Position;
    animPlayer_.Seek(rec.Animation, true);
    starDust_.Visible = rec.State is State.Prepare or State.Escape;
    return true;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref record_.Ref;
    state.LinearVelocity = rec.Velocity;
  }
  protected override ref int ShieldMut => ref record_.Mut.Shield;
}
