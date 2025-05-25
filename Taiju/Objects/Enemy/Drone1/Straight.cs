using System;
using Godot;
using Taiju.Objects.BulletServer.Server;
using Taiju.Util;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Enemy.Drone1;

public partial class Straight : EnemyBase {
  [Export] private Vector3 initialVelocity_ = new(-10.0f, 0.0f, 0.0f);
  private Vector3 rotatedInitialVelocity_;
  [Export(PropertyHint.Range, "0,30,")] private float seekDistance_ = 10.0f;
  [Export(PropertyHint.Range, "0,30,")] private float timeoutDuration_ = 3.0f;
  [Export(PropertyHint.Range, "0,360,")] private float maxRotateDegreePerSec_ = 120.0f;
  [Export(PropertyHint.Range, "0,20,")] private float returnDistance_ = 12.0f;
  [Export(PropertyHint.Range, "0,20,")] private float escapeDistance_ = 13.0f;
  [Export(PropertyHint.Range, "0,30,")] private float bulletSpeed_ = 15.0f;
  private const string SeekReq = "parameters/Seek/seek_request";
  private CircleBulletServer bulletServer_;

  //
  private enum State {
    Seek,
    Escape,
  }

  private Node3D body_;
  private AnimationTree animationTree_;
  private Dense<Record> record_;
  private bool swapEscapeDirection_;

  private record struct Record {
    public State State;
    public int Shield;
    public Vector3 Position;
    public Vector3 Velocity;
  }

  public override void _Ready() {
    base._Ready();
    Name = "Drone1/Straight";
    body_ = GetNode<Node3D>("Body")!;

    animationTree_ = GetNode<AnimationTree>("AnimationTree")!;
    animationTree_.Active = true;

    rotatedInitialVelocity_ = Vec.Rotate(initialVelocity_, Rotation.Z);
    Rotation = new Vector3(Rotation.X, Rotation.Y, 0.0f);

    record_ = new Dense<Record>(Clock, new Record {
      State = State.Seek,
      Shield = InitialShield,
      Position = Position,
      Velocity = rotatedInitialVelocity_,
    });

    swapEscapeDirection_ = Random.Shared.NextDouble() < 0.5;
    bulletServer_ = GetNode<CircleBulletServer>("/root/Root/Field/EnemyBullet/RedCircleBulletServer")!;
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
    var maxAngle = (float)(dt * Mathf.DegToRad(maxRotateDegreePerSec_));

    switch (rec.State) {
      case State.Seek: {
        rec.Velocity = rotatedInitialVelocity_;
        if ((soraPosition - currentPosition).Length() <= seekDistance_ || integrateTime > timeoutDuration_) {
          rec.State = State.Escape;
          bulletServer_.SpawnToSora(rec.Position, bulletSpeed_);
        }
      }
        break;

      case State.Escape: {
        if (swapEscapeDirection_) {
          rec.Velocity = Mover.Follow(Vector3.Right, rec.Velocity, -maxAngle);
        } else {
          rec.Velocity = Mover.Follow(Vector3.Right, rec.Velocity, maxAngle);
        }
      }
        break;

      default:
        throw new ArgumentOutOfRangeException();
    }

    { // Update godot states
      body_.Rotation = new Vector3(0, 0, Vec.Atan2(-rec.Velocity));
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
    body_.Rotation = new Vector3(0, 0, Vec.Atan2(-rec.Velocity));
    animationTree_.Set(SeekReq, integrateTime);
    return true;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref record_.Ref;
    state.LinearVelocity = rec.Velocity;
  }
  protected override ref int ShieldMut => ref record_.Mut.Shield;
}
