using Godot;
using Taiju.Reversible.Gd;

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

  protected bool IsOutOfView(float halfSize) {
    var pos = Camera.UnprojectPosition(GlobalPosition);
    return VisibleRect.HasPoint(pos + new Vector2(-halfSize, -halfSize)) &&
           VisibleRect.HasPoint(pos + new Vector2(-halfSize, +halfSize)) &&
           VisibleRect.HasPoint(pos + new Vector2(+halfSize, -halfSize)) && 
           VisibleRect.HasPoint(pos + new Vector2(+halfSize, +halfSize));
  }
}
