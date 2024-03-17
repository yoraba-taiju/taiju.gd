using System;
using Godot;
using Taiju.Objects.BulletServer.Servers;
using Taiju.Objects.Reversible.Value;
using Taiju.Util.Godot;
using Vector3 = Godot.Vector3;

namespace Taiju.Objects.Enemy.Drone2;
// https://code.ledyba.org/yoraba-taiju/taiju.unity/src/branch/magistra/Assets/Scripts/Enemy/Drone/Drone2.cs

public partial class Brain : EnemyBase {
  [Export(PropertyHint.Range, "0,100,1")] private int initialShield_ = 30;
  [Export(PropertyHint.Range, "0,360,")] private float maxRotateDegreePerSec_ = 180.0f;
  [Export(PropertyHint.Range, "0,20,")] private float seekSpeed_ = 7.0f;
  [Export(PropertyHint.Range, "0,40,")] private float escapeSpeed_ = 23.0f;
  [Export(PropertyHint.Range, "0,20")] private float timeToFire_ = 0.3f;
  [Export(PropertyHint.Range, "0,20,1")] private int fireCount_ = 5;
  [Export(PropertyHint.Range, "0,30,")] private float bulletSpeed_ = 15.0f;
  private CircleBulletServer circleBulletServer_;

  //
  private enum State {
    Seek,
    Fight,
    Sleep,
    Escape,
  }

  private Node3D body_;
  private Dense<Record> record_;
  private RandomNumberGenerator rand_ = new();

  private record struct Record {
    public int Shield;
    public State State;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Rotation;
    public int FireCount;
    public double TimeToFire;
    public double NextTimeToAction;
  }

  public override void _Ready() {
    base._Ready();
    body_ = GetNode<Node3D>("Body");
    record_ = new Dense<Record>(Clock, new Record {
      Shield = initialShield_,
      State = State.Seek,
      Position = Position,
      Velocity = new Vector3(-10.0f, 0.0f, 0.0f),
      Rotation = 0.0f,
      FireCount = fireCount_,
      TimeToFire = timeToFire_,
      NextTimeToAction = 0.0f,
    });
    circleBulletServer_ = GetNode<CircleBulletServer>("/root/Root/Field/EnemyBullet/CircleBulletServer");
  }
  
  public override bool _ProcessForward(double integrateTime, double dt) {
    base._ProcessForward(integrateTime, dt);
    ref var rec = ref record_.Mut;
    { // Record godot states
      rec.Position = Position;
    }
    var currentPosition = rec.Position;
    var soraPosition = Sora.Position;
    var maxAngle = (float)(dt * Mathf.DegToRad(maxRotateDegreePerSec_));
    var targetDirection = soraPosition - currentPosition;
    var targetDistance = targetDirection.Length();
    var currentRot = rec.Rotation - Mathf.Pi;
    var currentVelocity = rec.Velocity;
    var deltaAngle = Vec.DeltaAngle(rec.Rotation - Mathf.Pi, targetDirection);
    ref var nextTimeToAction = ref rec.NextTimeToAction;
    ref var state = ref rec.State;

    switch (state) {
      case State.Seek: {
        currentRot += Mathf.Clamp(deltaAngle, -maxAngle, maxAngle);
        // Set speed
        if (targetDistance > 10.0f) {
          currentVelocity =
            new Vector3(Mathf.Cos(currentRot), Mathf.Sin(currentRot), 0.0f) *
            (seekSpeed_ * Mathf.Exp(Mathf.Clamp(targetDistance - 10.0f, 0.0f, 0.5f)));
        } else {
          currentVelocity =
            new Vector3(Mathf.Cos(currentRot), Mathf.Sin(currentRot), 0.0f) *
            currentVelocity.Length() * Mathf.Pow(3.0f, -(float)dt);
        }
      }
        break;

      case State.Fight: {
        currentRot += Mathf.Clamp(deltaAngle, -maxAngle, maxAngle);
        currentVelocity *= Mathf.Exp((float)-dt);
        ref var timeToFire = ref rec.TimeToFire;
        timeToFire -= dt;
        if (timeToFire < 0.0) {
          var velocity3d = new Vector3(Mathf.Cos(currentRot), Mathf.Sin(currentRot), 0f);
          var velocity = new Vector2(velocity3d.X, velocity3d.Y) * bulletSpeed_;
          circleBulletServer_.Spawn(currentPosition + velocity3d * 1.2f, velocity);
          // Next
          timeToFire += timeToFire_;
        }
      }
        break;

      case State.Sleep: {
        // Do nothing
      }
        break;

      case State.Escape: {
        if (currentVelocity.Length() < escapeSpeed_) {
          currentVelocity = new Vector3(Mathf.Cos(currentRot), Mathf.Sin(currentRot), 0f) * escapeSpeed_;
        } else {
          currentVelocity *= Mathf.Exp((float)dt);
        }
      }
        break;

      default:
        throw new ArgumentOutOfRangeException();
    }


    if (nextTimeToAction < integrateTime) {
      switch (state) {
        case State.Seek:
          if (targetDistance >= 10.0f || Mathf.Abs(deltaAngle) >= Mathf.Pi / 60.0f) {
            state = State.Seek;
            nextTimeToAction += 1.0;
          } else {
            state = State.Fight;
            nextTimeToAction += 1.0;
          }
          break;

        case State.Fight:
          rec.FireCount--;
          state = State.Sleep;
          nextTimeToAction += 0.5;
          break;

        case State.Sleep:
          if (rec.FireCount >= 0) {
            state = State.Seek;
            nextTimeToAction += 1.0;
          } else {
            state = State.Escape;
            nextTimeToAction = double.PositiveInfinity;
          }
          break;

        case State.Escape:
          break;

        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    currentRot += Mathf.Pi;
    { // Record state
      rec.Rotation = currentRot;
      rec.Velocity = currentVelocity;
    }
    { // Update godot states
      body_.Rotation = new Vector3(0, 0, currentRot);
    }

    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    return LoadCurrentStatus(integrateTime);
  }


  private bool LoadCurrentStatus(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    Position = rec.Position;
    body_.Rotation = new Vector3(0, 0, rec.Rotation);
    return true;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref record_.Ref;
    state.LinearVelocity = rec.Velocity;
  }
  protected override ref int ShieldMut => ref record_.Mut.Shield;
}
