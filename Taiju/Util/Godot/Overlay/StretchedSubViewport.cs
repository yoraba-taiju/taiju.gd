using Godot;
using Taiju.UI.HUD;

namespace Taiju.Util.Godot.Overlay;

public partial class StretchedSubViewport : SubViewport {
  private HudParent hudParent_;
  private SubViewportContainer Container => GetParent<SubViewportContainer>()!;
  private Vector2I WindowSize => GetWindow().Size;

  public override void _Ready() {
    base._Ready();
    GetWindow().SizeChanged += OnWindowSizeChanged;
    SetContainerSize(WindowSize);
    hudParent_ = GetNode<HudParent>("/root/Root/Field/HUD")!;
    hudParent_.OnResize(WindowSize);
  }

  private void SetContainerSize(Vector2I size) {
    var container = Container;
    container.PivotOffset = Vector2.Zero;
    container.Position = Vector2.Zero;
    container.Size = size;
  }

  private void OnWindowSizeChanged() {
    SetContainerSize(WindowSize);
    hudParent_.OnResize(WindowSize);
  }
}
