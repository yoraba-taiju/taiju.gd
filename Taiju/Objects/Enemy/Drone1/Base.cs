using System;
using Godot;
using Taiju.Objects.BulletServer.Server;
using Taiju.Util;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Enemy.Drone1;

public abstract partial class Base<TParam> : EnemyBase
where
  TParam: struct
{
  private const string SeekReq = "parameters/Seek/seek_request";

  [Export] private Vector3 initialVelocity_ = new(-10.0f, 0.0f, 0.0f);

  // Current Status
  protected Vector3 Velocity;
  protected CircleBulletServer BulletServer;

  // Nodes
  protected Node3D Body;
  protected AnimationTree AnimationTree;
  protected Dense<RecordType> Record;

  protected record struct RecordType {
    public int Shield;
    public Vector3 Position;
    public Vector3 Velocity;
    public TParam Param;
  }

  public override void _Ready() {
    base._Ready();
    Body = GetNode<Node3D>("Body")!;

    AnimationTree = GetNode<AnimationTree>("AnimationTree")!;
    AnimationTree.Active = true;

    Velocity = Vec.Rotate(initialVelocity_, Rotation.Z);
    Rotation = new Vector3(Rotation.X, Rotation.Y, 0.0f);

    Record = new Dense<RecordType>(Clock, new RecordType {
      Shield = InitialShield,
      Position = Position,
      Velocity = Velocity,
      Param = new TParam(),
    });

    BulletServer = GetNode<CircleBulletServer>("/root/Root/Field/EnemyBullet/RedCircleBulletServer")!;
    AnimationTree.Set(SeekReq, 0f);
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var rec = ref Record.Mut;

    { // Record godot states
      rec.Position = Position;
    }
    // Save states
    rec.Velocity = Velocity;
    { // Update godot states
      Body.Rotation = new Vector3(0, 0, Vec.Atan2(-rec.Velocity));
    }

    return base._ProcessForward(integrateTime, dt);
  }
  
  public override bool _ProcessBack(double integrateTime) {
    base._ProcessBack(integrateTime);
    return LoadCurrentStatus(integrateTime);
  }

  public override bool _ProcessLeap(double integrateTime) {
    base._ProcessLeap(integrateTime);
    return LoadCurrentStatus(integrateTime);
  }

  private bool LoadCurrentStatus(double integrateTime) {
    ref readonly var rec = ref Record.Ref;
    Position = rec.Position;
    Body.Rotation = new Vector3(0, 0, Vec.Atan2(-rec.Velocity));
    AnimationTree.Set(SeekReq, integrateTime);
    return true;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref Record.Ref;
    state.LinearVelocity = rec.Velocity;
  }
  protected override ref int ShieldMut => ref Record.Mut.Shield;
}
