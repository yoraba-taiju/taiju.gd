using System;
using Godot;

namespace Taiju.Util.Gd.Overlay;

public partial class StretchedControl : Control {
  public override void _Ready() {
    base._Ready();
    OnViewportSizeChanged();
    GetParent().GetViewport().SizeChanged += OnViewportSizeChanged;
  }

  private void OnViewportSizeChanged() {
    Position = Vector2.Zero;
    var parent = GetParent().GetViewport();
    Size = parent switch {
      SubViewport s => s.Size,
      Window w => w.Size,
      _ => throw new ArgumentOutOfRangeException(),
    };
  }
}
