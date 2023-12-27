using Godot;

namespace Taiju.Scenes;

public partial class Spawner : Node3D {
  private Node3D Field { get; set; }

  public override void _Ready() {
    base._Ready();
    Field = GetNode<Node3D>("/root/Root/Field/Enemy");
  }

  private void Play(PackedScene packedNode, Vector2 at) {
    var node = packedNode.Instantiate<Node3D>();
    node.Translate(new Vector3(at.X, at.Y, 0.0f));
    Field.AddChild(node);
  }

  private void PlayRush(PackedScene rushScene) {
    var scene = rushScene.Instantiate<Node3D>();
    scene.QueueFree();
    var children = scene.GetChildren();
    foreach (var child in children) {
      scene.RemoveChild(child);
      Field.AddChild(child);
    }
  }
}
