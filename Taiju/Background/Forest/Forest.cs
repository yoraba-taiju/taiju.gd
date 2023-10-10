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
    var prop = mat_.Get("uv1_offset").AsVector3();
    prop.X = (float)(0.1f * integrateTime);
    mat_.Set("uv1_offset", prop);
  }
}
