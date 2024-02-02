using System;
using Godot;
using Taiju.Objects.BulletServer.Servers;
using Taiju.Objects.Reversible.Value;
using Taiju.Util.Godot;

namespace Taiju.Objects.Enemy.Missile0;

public partial class Brain : EnemyBase {
  [Export(PropertyHint.Range, "0,360,")] private float maxRotateDegreePerSec_ = 180.0f;
  [Export(PropertyHint.Range, "0,10,")] private float appearTime_ = 0.5f;
  //
  private enum State {
    Appear,
    Attack,
  }

  private Node3D body_;
  private CollisionShape3D shape_;
  private Quaternion shapeRot_;
  private RandomNumberGenerator rand_ = new();
  public Vector3 StartPosition = new (15, 7, 0);
  public Vector3 InitialPosition;
  private Dense<Record> record_;

  private record struct Record {
    public int Shield;
    public State State;
    public Vector3 Position;
    public Vector3 Velocity;
  }

  public override void _Ready() {
    base._Ready();
    body_ = GetNode<Node3D>("Body")!;
    shape_ = GetNode<CollisionShape3D>("Shape")!;
    shapeRot_ = shape_.Quaternion;
    InitialPosition = Position;
    record_ = new Dense<Record>(Clock, new Record {
      Shield = 30,
      State = State.Appear,
      Position = Position,
      Velocity = new Vector3(-10.0f, 0.0f, 0.0f),
    });
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    base._ProcessForward(integrateTime, dt);
    
    ref var rec = ref record_.Mut;
    { // Record godot states
      rec.Position = Position;
    }
    var delta = Sora.Position - Position;
    delta.Z = 0;
    var maxAngle = (float)(dt * maxRotateDegreePerSec_);
    switch (rec.State) {
      case State.Appear: {
        if (integrateTime <= appearTime_) {
          var progress = integrateTime / appearTime_;
          progress = Math.Pow(progress, 1.0/1.8);
          rec.Position = InitialPosition + (StartPosition - InitialPosition) * (float)progress;
          rec.Velocity = Mover.Follow(delta, rec.Velocity, maxAngle);
          Position = rec.Position;
        } else {
          rec.State = State.Attack;
          rec.Position = StartPosition;
          rec.Velocity = delta.Normalized() * 10.0f;
        }
      }
        break;
      case State.Attack: {
        rec.Velocity = Mover.Follow(delta, rec.Velocity, maxAngle);
      }
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }

    { // Update godot states
      var q =
        Quaternion.FromEuler(new Vector3(0, 0, Mathf.DegToRad(Vec.Atan2(-rec.Velocity)))) *
        Quaternion.FromEuler(new Vector3(Mathf.DegToRad((float)(integrateTime * 300.0)), 0, 0));
      body_.Quaternion = q;
      shape_.Quaternion = q * shapeRot_;
    }
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    return LoadCurrentStatus(integrateTime);
  }

  private bool LoadCurrentStatus(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    Position = rec.Position;
    var q =
      Quaternion.FromEuler(new Vector3(0, 0, Mathf.DegToRad(Vec.Atan2(-rec.Velocity)))) *
      Quaternion.FromEuler(new Vector3(Mathf.DegToRad((float)(integrateTime * 300.0)), 0, 0));
    body_.Quaternion = q;
    shape_.Quaternion = q * shapeRot_;
    return true;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref record_.Ref;
    state.LinearVelocity = rec.Velocity;
  }
  protected override ref int ShieldMut => ref record_.Mut.Shield;
}
