using Taiju.Util;

namespace Taiju.Objects.Enemy.Drone0;
using Godot;
using Taiju.Objects.BulletServer.Server;
using Taiju.Util.Reversible.Value;

public abstract partial class Base<TParam> : EnemyBase
  where
TParam: struct
{
  protected Node3D Body;
  private AnimationPlayer animPlayer_;
  protected Dense<RecordType> Record;
  protected CircleBulletServer BulletServer;

  protected record struct RecordType {
    public int Shield;
    public Vector3 Position;
    public Vector3 Rotation;
    public double Animation;
    public TParam Param;
  }
  public override void _Ready() {
    base._Ready();

    Body = GetNode<Node3D>("Body")!;
    Rotation = new Vector3(Rotation.X, Rotation.Y, 0.0f);
    Record = new Dense<RecordType>(Clock, new RecordType {
      Shield = InitialShield,
      Position = Position,
      Animation = 0.0,
      Param = new TParam(),
    });
    var model = Body.GetNode<Node3D>("Model")!;
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
      rec.Rotation = Body.Rotation;
      rec.Animation = animPlayer_.CurrentAnimationPosition;
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
    Body.Rotation = rec.Rotation;
    animPlayer_.Seek(rec.Animation, true, true);
    return true;
  }

  protected override ref int ShieldMut => ref Record.Mut.Shield;

}
