using System;
using Godot;
using Taiju.Util;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Enemy.Missile0;

public partial class Straight : EnemyBase {
  [Export(PropertyHint.Range, "0,100,")] private float speed_ = 36.0f;
  
  private RandomNumberGenerator rng_ = new();

  private Vector3 velocity_;

  private Dense<Record> record_;
  private Node3D body_;
  private Node3D model_;
  private CollisionShape3D shape_;
  private Quaternion shapeRotOriginal_;
  private float initialRotationRad_;

  private record struct Record {
    public int Shield;
    public Vector3 Position;
  }

  public override void _Ready() {
    base._Ready();
    Name = "Missile0/Straight";
    velocity_ = new Vector3(-speed_, 0.0f, 0.0f);
    body_ = GetNode<Node3D>("Body")!;
    model_ = GetNode<Node3D>("Body/Model")!;
    shape_ = GetNode<CollisionShape3D>("Shape")!;
    shapeRotOriginal_ = shape_.Quaternion;
    initialRotationRad_ = rng_.RandfRange(0.0f, (float)Math.PI * 2);
    BodySize = new Vector2(4.0f, 2.0f);
    record_ = new Dense<Record>(Clock, new Record {
      Shield = InitialShield,
      Position = Position,
    });
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    base._ProcessForward(integrateTime, dt);
    
    ref var rec = ref record_.Mut;
    { // Record godot states
      rec.Position = Position;
      SetPose(velocity_, integrateTime);
    }
    return true;
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
    ref readonly var rec = ref record_.Ref;
    Position = rec.Position;
    SetPose(velocity_, integrateTime);
    return true;
  }

  private void SetPose(Vector3 velocity, double integrateTime) {
    var q = Quaternion.FromEuler(new Vector3(0, 0, Vec.Atan2(-velocity)));
    var v = Quaternion.FromEuler(new Vector3((float)(integrateTime * Mathf.Pi * 1.7f) + initialRotationRad_, 0, 0));
    body_.Quaternion = q;
    shape_.Quaternion = q * shapeRotOriginal_;
    model_.Quaternion = v;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref record_.Ref;
    state.LinearVelocity = velocity_;
  }

  protected override ref int ShieldMut => ref record_.Mut.Shield;
}
