using Godot;
using Taiju.Reversible.GD;

namespace Taiju.Objects.Enemy; 

public abstract partial class EnemyBase : ReversibleRigidBody3D {
  protected Viewport Viewport;
  protected Camera3D Camera;
  protected Rect2 VisibleRect;
  public override void _Ready() {
    base._Ready();
    Viewport = GetViewport();
    Camera = Viewport.GetCamera3D();
    VisibleRect = Viewport.GetVisibleRect();
  }

  protected bool IsOutOfView(float size) {
    var pos = Camera.UnprojectPosition(GlobalPosition);
    var half = size / 2.0f;
    return VisibleRect.HasPoint(pos + new Vector2(-half, -half)) &&
           VisibleRect.HasPoint(pos + new Vector2(-half, +half)) &&
           VisibleRect.HasPoint(pos + new Vector2(+half, -half)) && 
           VisibleRect.HasPoint(pos + new Vector2(+half, +half));
  }
}
