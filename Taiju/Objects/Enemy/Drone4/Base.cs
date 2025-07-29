using Godot;
using Taiju.Objects.Effect;
using Taiju.Util;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Enemy.Drone4;

public abstract partial class Base<TParam> : EnemyBase
  where
TParam: struct
{
  // Nodes
  protected AnimationPlayer AnimPlayer;
  protected StarDust StarDust;
  
  // Childs
  protected Node3D Body;
  protected CollisionShape3D Shape;

  public struct RecordType {
    public int Shield;
    public Vector3 Position;
    public float Speed;
    public Vector3 Direction;
    public double Animation;
    public TParam Param;
  }

  public Dense<RecordType> Record;

  public override void _Ready() {
    base._Ready();
    Record = new Dense<RecordType>(Clock, new RecordType {
      Shield = InitialShield,
      Position = Position,
      Speed = 0.0f, // Please set in subclass.
      Direction = new Vector3(), // also
      Param = new TParam(),
    });
    AnimPlayer = GetNode<AnimationPlayer>("Body/Drone4/AnimationPlayer")!;
    AnimPlayer.CurrentAnimation = "Shaft_Rotate";
    StarDust = GetNode<StarDust>("Body/StarDust")!;
    Body = GetNode<Node3D>("Body")!;
    Shape = GetNode<CollisionShape3D>("Shape")!;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var rec = ref Record.Mut;
    { // Save Godot Status
      rec.Position = Position;
      rec.Animation = AnimPlayer.CurrentAnimationPosition;
    }
    { // Set Godot Status
      var rot = new Vector3(0, 0, Vec.Atan2(-rec.Direction));
      Body.Rotation = rot;
      Shape.Rotation = rot;
    }
    return base._ProcessForward(integrateTime, dt);
  }

  public override bool _ProcessBack(double integrateTime) {
    LoadCurrentStatus(integrateTime);
    return base._ProcessBack(integrateTime);
  }

  public override bool _ProcessLeap(double integrateTime) {
    LoadCurrentStatus(integrateTime);
    return base._ProcessLeap(integrateTime);
  }

  private void LoadCurrentStatus(double integrateTime) {
    ref readonly var rec = ref Record.Ref;
    Position = rec.Position;
    AnimPlayer.Seek(rec.Animation);
    var rot = new Vector3(0, 0, Vec.Atan2(-rec.Direction));
    Body.Rotation = rot;
    Shape.Rotation = rot;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref Record.Ref;
    state.LinearVelocity = rec.Direction.Normalized() * rec.Speed;
  }

  protected override ref int ShieldMut => ref Record.Mut.Shield;
}
