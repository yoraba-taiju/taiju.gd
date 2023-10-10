using Godot;
using Taiju.Reversible.Gd;

namespace Taiju.Objects.Bullets;

public abstract partial class ServerBase : ReversibleNode3D {
  [Export] private Mesh mesh_;
  private MultiMeshInstance3D multiMeshInstance3D_;
  private MultiMesh multiMesh_;
  public override void _Ready() {
    base._Ready();
    multiMeshInstance3D_ = GetNode<MultiMeshInstance3D>("Mesh");
    multiMesh_ = new MultiMesh();
    multiMeshInstance3D_.Multimesh = multiMesh_;
    multiMesh_.Mesh = mesh_;
    multiMesh_.UseColors = true;
  }
}
