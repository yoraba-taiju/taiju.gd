using Godot;
using Taiju.Util.Reversible.Godot;

namespace Taiju.Scenes.Stages.Stage01;

public partial class Forest : ReversibleNode3D {
  [Export] private float scrollSpeed_ = 0.075f;
  private Material material_;

  public override void _Ready() {
    base._Ready();
    var meshInstance = GetNode<MeshInstance3D>("Mesh")!;
    material_ = meshInstance.GetActiveMaterial(0)!;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    return false; // Use raw
  }

  public override bool _ProcessBack(double integrateTime) {
    return false; // Use raw
  }

  public override bool _ProcessLeap(double integrateTime) {
    return false; // Use raw
  }

  public override void _ProcessRaw(double integrateTime, double dt) {
    material_.Set("uv1_offset", new Vector3((float)(scrollSpeed_ * integrateTime), 0.0f, 0.0f));
  }
}
