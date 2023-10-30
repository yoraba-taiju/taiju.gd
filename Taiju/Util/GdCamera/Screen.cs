using System;
using Godot;

namespace Taiju.Util.GdCamera; 

public partial class Screen : Control {
  public override void _Ready() {
    base._Ready();
    OnViewportSizeChanged();
    GetParent().GetViewport().SizeChanged += OnViewportSizeChanged;
  }

  private void OnViewportSizeChanged() {
    var parent = GetParent().GetViewport();
    Size = parent switch {
      SubViewport s => s.Size,
      Window w => w.Size,
      _ => throw new ArgumentOutOfRangeException(),
    };
  }
}
