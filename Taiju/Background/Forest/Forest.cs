using Godot;
using Taiju.Reversible.GD;
using Taiju.Reversible.Value;

namespace Taiju.Background.Forest;

public partial class Forest : ReversibleNode3D {
  private Material mat_;

  public override void _Ready() {
    base._Ready();
    var meshInstance = GetNode<MeshInstance3D>("Mesh");
    mat_ = meshInstance.GetActiveMaterial(0);
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    return false;
  }

  public override bool _ProcessBack() {
    return false;
  }

  public override bool _ProcessLeap() {
    return false;
  }

  public override void _ProcessRaw(double integrateTime) {
    mat_.Set("uv1_offset", new Vector3((float)(0.1 * integrateTime), 0.0f, 0.0f));
  }
}
