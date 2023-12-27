using Godot;

namespace Taiju.Scenes;

public partial class Spawner : Node3D {
  private Node3D field_;
  public override void _Ready() {
    base._Ready();
    field_ = GetNode<Node3D>("/root/Root/Field/Enemy");
  }

  private void PlayRush(PackedScene packedScene) {
    var scene = packedScene.Instantiate<Node3D>();
    var all = scene.GetChildren();
    foreach (var child in all) {
      scene.RemoveChild(child);
      field_.AddChild(child);
    }
    scene.QueueFree();
  }
}
