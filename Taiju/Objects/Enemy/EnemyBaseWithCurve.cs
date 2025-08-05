using Godot;
using Taiju.Util.Godot;

namespace Taiju.Objects.Enemy;

public abstract partial class EnemyBaseWithCurve : EnemyBase {
  private Curve3D curve_;
  private float curveLength_;
  public Curve3D Curve {
    protected get => curve_;
    set {
      curve_ = value;
      curveLength_ = value.GetBakedLength();
    }
  }
  protected Camera Camera;

  public override void _Ready() {
    base._Ready();
    Camera = GetNode<Camera>("/root/Root/MainCamera")!;
  }

  protected Vector3 CalcPosition(float distance) {
    var offset = Mathf.Clamp(distance / curveLength_, 0.0f, 1.0f);
    return curve_.SampleBaked(offset);
  }
}
