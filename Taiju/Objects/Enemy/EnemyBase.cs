using Godot;
using Taiju.Objects.Witch;
using Taiju.Reversible.Gd;

namespace Taiju.Objects.Enemy; 

public abstract partial class EnemyBase : ReversibleRigidBody3D {
  protected Sora Sora { get; private set; }
  protected Viewport Viewport;
  protected Camera3D Camera;
  protected Rect2 VisibleRect;
  public override void _Ready() {
    base._Ready();
    Viewport = GetViewport();
    Camera = Viewport.GetCamera3D();
    VisibleRect = Viewport.GetVisibleRect();
    Sora = GetNode<Sora>("/root/Root/Field/Witch/Sora");
  }

  protected bool IsOutOfView(float halfSize) {
    var pos = Camera.UnprojectPosition(GlobalPosition);
    return VisibleRect.HasPoint(pos + new Vector2(-halfSize, -halfSize)) &&
           VisibleRect.HasPoint(pos + new Vector2(-halfSize, +halfSize)) &&
           VisibleRect.HasPoint(pos + new Vector2(+halfSize, -halfSize)) && 
           VisibleRect.HasPoint(pos + new Vector2(+halfSize, +halfSize));
  }
}
