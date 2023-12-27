using Godot;

namespace Taiju.Objects.Reversible.Godot;

public partial class ReversibleSpawner : Node3D {
  protected Node3D Field { get; private set; }
  public override void _Ready() {
    base._Ready();
    Field = GetNode<Node3D>("/root/Root/Field/Enemy");
  }
}
