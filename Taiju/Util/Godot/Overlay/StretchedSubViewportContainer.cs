using System;
using Godot;

namespace Taiju.Util.Godot.Overlay;

public partial class StretchedSubViewportContainer : SubViewportContainer {
  public override void _Ready() {
    base._Ready();
    OnViewportSizeChanged();
    var viewPort = GetParent()!.GetViewport()!;
    viewPort.SizeChanged += OnViewportSizeChanged;
  }

  private void OnViewportSizeChanged() {
    PivotOffset = Vector2.Zero;
    Position = Vector2.Zero;
    var viewPort = GetParent()!.GetViewport()!;
    Size = viewPort switch {
      SubViewport s => s.Size,
      Window w => w.Size,
      _ => throw new ArgumentOutOfRangeException(),
    };
  }
}
