using Godot;

namespace Taiju.Background.Forest;

public partial class Forest : MeshInstance3D {
  private Material mat_;

  public override void _Ready() {
    mat_ = GetActiveMaterial(0);
  }

  public override void _Process(double delta) {
    var prop = mat_.Get("uv1_offset").AsVector3();
    prop.X += (float)(0.1f * delta);
    // prop.Y += (float)(0.0f * delta);
    mat_.Set("uv1_offset", prop);
  }
}
