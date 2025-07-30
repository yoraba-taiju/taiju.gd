using Taiju.Util;

namespace Taiju.Objects.Enemy.Drone0;
using Godot;
using Taiju.Objects.BulletServer.Server;
using Taiju.Util.Reversible.Value;

public abstract partial class Base<TParam> : EnemyBase
  where
TParam: struct
{
  [Export] private Vector3 initialVelocity_ = new(-10.0f, 0.0f, 0.0f);
  protected Vector3 Velocity;

  private Node3D body_;
  private AnimationPlayer animPlayer_;
  protected Dense<RecordType> Record;
  protected CircleBulletServer BulletServer;

  protected record struct RecordType {
    public int Shield;
    public Vector3 Position;
    public Vector3 Velocity;
    public double Animation;
    public TParam Param;
  }
  public override void _Ready() {
    base._Ready();

    body_ = GetNode<Node3D>("Body")!;
    Velocity = Vec.Rotate(initialVelocity_, Rotation.Z);
    Rotation = new Vector3(Rotation.X, Rotation.Y, 0.0f);
    Record = new Dense<RecordType>(Clock, new RecordType {
      Shield = InitialShield,
      Position = Position,
      Velocity = Velocity,
      Animation = 0.0,
      Param = new TParam(),
    });
    var model = body_.GetNode<Node3D>("Model")!;
    animPlayer_ = model.GetNode<AnimationPlayer>("AnimationPlayer")!;
    animPlayer_.PlaybackActive = true;
    animPlayer_.Play("Rotate");
    BulletServer = GetNode<CircleBulletServer>("/root/Root/Field/EnemyBullet/RedCircleBulletServer")!;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    base._ProcessForward(integrateTime, dt);
    ref var rec = ref Record.Mut;
    { // Record godot states
      rec.Position = Position;
      rec.Animation = animPlayer_.CurrentAnimationPosition;
    }
    // Save states
    rec.Velocity = Velocity;
    { // Set godot states.
      body_.Rotation = new Vector3(0, 0, Vec.Atan2(-rec.Velocity));
    }
    return true;
  }
    
  public override bool _ProcessBack(double integrateTime) {
    base._ProcessBack(integrateTime);
    return LoadCurrentStatus();
  }

  public override bool _ProcessLeap(double integrateTime) {
    base._ProcessLeap(integrateTime);
    return LoadCurrentStatus();
  }

  private bool LoadCurrentStatus() {
    ref readonly var rec = ref Record.Ref;
    Position = rec.Position;
    body_.Rotation = new Vector3(0, 0, Vec.Atan2(-rec.Velocity));
    animPlayer_.Seek(rec.Animation, true, true);
    return true;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref Record.Ref;
    state.LinearVelocity = rec.Velocity;
  }
  protected override ref int ShieldMut => ref Record.Mut.Shield;

}
