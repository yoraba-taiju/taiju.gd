using System;
using Godot;
using Taiju.Objects.BulletServer.Servers;
using Taiju.Objects.Reversible.Value;
using Taiju.Util.Godot;

namespace Taiju.Objects.Enemy.Drone2;

public partial class Brain : EnemyBase {
  [Export(PropertyHint.Range, "0,360,")] private float maxRotateDegreePerSec_ = 180.0f;
  [Export(PropertyHint.Range, "0,20,")] private float seekSpeed_ = 7.0f;
  [Export(PropertyHint.Range, "0,20,")] private float escapeSpeed_ = 12.0f;
  [Export(PropertyHint.Range, "0,20")] private float timeToFire_ = 0.3f;
  [Export(PropertyHint.Range, "0,20,1")] private int fireCount_ = 5;
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
    public int FireCount;
    public double TimeToFire;
  }

  public override void _Ready() {
    base._Ready();
    body_ = GetNode<Node3D>("Body");
    record_ = new Dense<Record>(Clock, new Record {
      Shield = 4,
      State = State.Seek,
      Position = Position,
      Velocity = new Vector3(-10.0f, 0.0f, 0.0f),
      FireCount = fireCount_,
      TimeToFire = timeToFire_,
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
    var maxAngle = (float)(dt * maxRotateDegreePerSec_);

    switch (rec.State) {
      case State.Seek:
        break;

      case State.Fight:
        break;

      case State.Sleep:
        break;

      case State.Escape:
        break;

      default:
        throw new ArgumentOutOfRangeException();
    }

    { // Update godot states
      body_.Rotation = new Vector3(0, 0, Mathf.DegToRad(Vec.Atan2(-rec.Velocity)));
    }

    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    return LoadCurrentStatus(integrateTime);
  }

  private bool LoadCurrentStatus(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    Position = rec.Position;
    body_.Rotation = new Vector3(0, 0, Mathf.DegToRad(Vec.Atan2(-rec.Velocity)));
    return true;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref record_.Ref;
    state.LinearVelocity = rec.Velocity;
  }
  protected override ref int ShieldMut => ref record_.Mut.Shield;
}
