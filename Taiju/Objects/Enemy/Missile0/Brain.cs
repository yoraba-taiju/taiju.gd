using System;
using Godot;
using Taiju.Objects.Reversible.Value;
using Taiju.Util.Godot;

namespace Taiju.Objects.Enemy.Missile0;

public partial class Brain : EnemyBase {
  [Export(PropertyHint.Range, "0,360,")] private float maxRotateDegreePerSec_ = 60.0f;
  [Export(PropertyHint.Range, "0,10,")] private float appearTime_ = 0.5f;
  [Export(PropertyHint.Range, "0,100,")] private float speed_ = 20.0f;
  //
  private enum State {
    Appear,
    Follow,
    Attack,
  }

  private Node3D body_;
  private Node3D model_;
  private CollisionShape3D shape_;
  private Quaternion shapeRot_;
  private RandomNumberGenerator rand_ = new();
  private Vector3 appearPosition_;
  private Vector3 initialPosition_;
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
    model_ = GetNode<Node3D>("Body/Model")!;
    shape_ = GetNode<CollisionShape3D>("Shape")!;
    shapeRot_ = shape_.Quaternion;
    initialPosition_ = Position;
    appearPosition_ = new Vector3(rand_.RandfRange(15, 17), rand_.RandfRange(-10, 10), 0.0f);
    record_ = new Dense<Record>(Clock, new Record {
      Shield = 5,
      State = State.Appear,
      Position = Position,
      Velocity = new Vector3(-speed_, 0.0f, 0.0f),
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
    var maxAngle = (float)(dt * Mathf.DegToRad(maxRotateDegreePerSec_));
    switch (rec.State) {
      case State.Appear: {
        if (integrateTime <= appearTime_) {
          var progress = integrateTime / appearTime_;
          progress = Math.Pow(progress, 1.0/1.8);
          rec.Position = initialPosition_ + (appearPosition_ - initialPosition_) * (float)progress;
          rec.Velocity = Mover.Follow(delta, rec.Velocity, maxAngle);
          Position = rec.Position;
        } else {
          rec.State = State.Follow;
          rec.Position = appearPosition_;
          rec.Velocity = delta.Normalized() * speed_;
        }
      }
        break;
      case State.Follow: {
        if (delta.Length() >= 3.0f) {
          rec.Velocity = Mover.Follow(delta, rec.Velocity, maxAngle).Normalized() * speed_ * MathF.Exp((float)(integrateTime - appearTime_));
        } else {
          rec.State = State.Attack;
        }
      }
        break;
      case State.Attack:
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }

    { // Update godot states
      SetVelocity(rec.Velocity, integrateTime);
    }
    return true;
  }


  public override bool _ProcessBack(double integrateTime) {
    return LoadCurrentStatus(integrateTime);
  }

  private bool LoadCurrentStatus(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    Position = rec.Position;
    SetVelocity(rec.Velocity, integrateTime);
    return true;
  }

  private void SetVelocity(Vector3 velocity, double integrateTime) {
    var q = Quaternion.FromEuler(new Vector3(0, 0, Vec.Atan2(-velocity)));
    var v = Quaternion.FromEuler(new Vector3((float)(integrateTime * Mathf.Pi * 1.7f), 0, 0));
    body_.Quaternion = q;
    shape_.Quaternion = q * shapeRot_;
    model_.Quaternion = v;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref record_.Ref;
    state.LinearVelocity = rec.Velocity;
  }
  protected override ref int ShieldMut => ref record_.Mut.Shield;
}
