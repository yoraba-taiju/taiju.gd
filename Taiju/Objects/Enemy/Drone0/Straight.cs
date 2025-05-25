using Godot;
using Taiju.Objects.BulletServer.Server;
using Taiju.Util;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Enemy.Drone0;

public partial class Straight : EnemyBase {
  [Export(PropertyHint.Flags)] private bool shotToSora_;
  [Export(PropertyHint.Range, "0,30,")] private float bulletSpeed_ = 15.0f;
  [Export] private Vector3 initialVelocity_ = new(-10.0f, 0.0f, 0.0f);
  private Vector3 rotatedInitialVelocity_;

  private Node3D body_;
  private AnimationPlayer animPlayer_;
  private Dense<Record> record_;
  private CircleBulletServer bulletServer_;

  private record struct Record {
    public int Shield;
    public Vector3 Position;
    public Vector3 Velocity;
    public double Animation;
    public bool ShotToSora;
  }

  public override void _Ready() {
    base._Ready();
    Name = "Drone0/Straight";
    body_ = GetNode<Node3D>("Body")!;
    rotatedInitialVelocity_ = Vec.Rotate(initialVelocity_, Rotation.Z);
    Rotation = new Vector3(Rotation.X, Rotation.Y, 0.0f);
    record_ = new Dense<Record>(Clock, new Record {
      Shield = InitialShield,
      Position = Position,
      Velocity = rotatedInitialVelocity_,
      Animation = 0.0,
      ShotToSora = shotToSora_,
    });
    var model = body_.GetNode<Node3D>("Model")!;
    animPlayer_ = model.GetNode<AnimationPlayer>("AnimationPlayer")!;
    var anim = animPlayer_.GetAnimation("Rotate")!;
    anim.LoopMode = Animation.LoopModeEnum.Linear;
    animPlayer_.PlaybackActive = true;
    animPlayer_.Play("Rotate");
    bulletServer_ = GetNode<CircleBulletServer>("/root/Root/Field/EnemyBullet/RedCircleBulletServer")!;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    base._ProcessForward(integrateTime, dt);
    ref var rec = ref record_.Mut;
    { // Record godot states
      rec.Position = Position;
      rec.Animation = animPlayer_.CurrentAnimationPosition;
    }
    rec.Velocity = rotatedInitialVelocity_;
    if (Position.X <= 18.0f && rec.ShotToSora) {
      rec.ShotToSora = false;
      bulletServer_.SpawnToSora(Position, bulletSpeed_);
    }

    { // Set godot states.
      body_.Rotation = new Vector3(0, 0, Vec.Atan2(-rec.Velocity));
    }

    return true;
  }
  
  public override bool _ProcessBack(double integrateTime) {
    base._ProcessBack(integrateTime);
    return LoadCurrentStatus();
  }


  private bool LoadCurrentStatus() {
    ref readonly var rec = ref record_.Ref;
    Position = rec.Position;
    body_.Rotation = new Vector3(0, 0, Vec.Atan2(-rec.Velocity));
    animPlayer_.Seek(rec.Animation, true);
    return true;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref record_.Ref;
    state.LinearVelocity = rec.Velocity;
  }
  protected override ref int ShieldMut => ref record_.Mut.Shield;
}
