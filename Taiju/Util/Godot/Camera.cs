using System;
using Godot;

namespace Taiju.Util.Godot;

public partial class Camera : Camera3D {
  public Vector2 HalfScreenSize { private set; get; }
  public override void _Ready() {
    base._Ready();
    OnWindowSizeChanged();
  }

  public override void _EnterTree() {
    base._EnterTree();
    GetWindow().SizeChanged += OnWindowSizeChanged;
  }

  public override void _ExitTree() {
    base._ExitTree();
    GetWindow().SizeChanged -= OnWindowSizeChanged;
  }

  private void OnWindowSizeChanged() {
    var size = ProjectPosition(new Vector2(), Position.Z);
    HalfScreenSize = new Vector2(Mathf.Abs(size.X), Mathf.Abs(size.Y));
  }
}
