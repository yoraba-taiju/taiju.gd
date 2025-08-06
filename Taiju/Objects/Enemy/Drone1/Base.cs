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

  // Node
  protected CircleBulletServer BulletServer;

  // Child Nodes
  protected Node3D Body;
  private AnimationTree animationTree_;

  protected record struct RecordType {
    public int Shield;
    public Vector3 Position;
    public Vector3 Rotation;
    public TParam Param;
  }
  protected Dense<RecordType> Record;

  public override void _Ready() {
    base._Ready();

    // Child nodes
    Body = GetNode<Node3D>("Body")!;
    animationTree_ = GetNode<AnimationTree>("AnimationTree")!;
    animationTree_.Active = true;

    Rotation = new Vector3(Rotation.X, Rotation.Y, 0.0f);

    Record = new Dense<RecordType>(Clock, new RecordType {
      Shield = InitialShield,
      Position = Position,
      Param = new TParam(),
    });

    BulletServer = GetNode<CircleBulletServer>("/root/Root/Field/EnemyBullet/RedCircleBulletServer")!;
    animationTree_.Set(SeekReq, 0f);
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var rec = ref Record.Mut;

    { // Record godot states
      rec.Position = Position;
      rec.Rotation = Body.Rotation;
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

  protected virtual bool LoadCurrentStatus(double integrateTime) {
    ref readonly var rec = ref Record.Ref;
    Position = rec.Position;
    Body.Rotation = rec.Rotation;
    animationTree_.Set(SeekReq, integrateTime);
    return true;
  }

  protected override ref int ShieldMut => ref Record.Mut.Shield;
}
