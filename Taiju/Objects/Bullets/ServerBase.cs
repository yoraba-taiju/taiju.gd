using Godot;

namespace Taiju.Objects.Bullets;

public abstract partial class ServerBase : MultiMeshInstance3D {
  [Export] private Mesh mesh_;
  private MultiMesh multiMesh_;
  public override void _Ready() {
    multiMesh_ = new MultiMesh();
    Multimesh = multiMesh_;
    multiMesh_.Mesh = mesh_;
    multiMesh_.UseColors = true;
  }
}
