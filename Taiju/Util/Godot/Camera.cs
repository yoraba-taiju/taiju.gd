using Godot;

namespace Taiju.Util.Godot;

public partial class Camera : Camera3D {
  public Vector2 HalfScreenSize { private set; get; }
  public override void _Ready() {
    base._Ready();
    var win = GetWindow();
    win.SizeChanged += OnWindowSizeChanged;
    OnWindowSizeChanged();
  }
  private void OnWindowSizeChanged() {
    var size = ProjectPosition(new Vector2(), Position.Z);
    HalfScreenSize = new Vector2(Mathf.Abs(size.X), Mathf.Abs(size.Y));
  }
}
