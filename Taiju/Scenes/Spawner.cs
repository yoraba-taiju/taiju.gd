using Godot;

namespace Taiju.Scenes;

public partial class Spawner : Node3D {
  private Node3D field_;

  public override void _Ready() {
    base._Ready();
    field_ = GetNode<Node3D>("/root/Root/Field/Enemy")!;
  }

  private void Play(PackedScene packedNode, Vector2 at) {
    var node = packedNode.Instantiate<Node3D>();
    node.Translate(new Vector3(at.X, at.Y, 0.0f));
    field_.AddChild(node);
  }

  private void PlayRush(PackedScene packedScene) {
    var rush = packedScene.Instantiate<Node3D>();
    rush.QueueFree();
    var children = rush.GetChildren();
    foreach (var child in children) {
      rush.RemoveChild(child);
      field_.AddChild(child);
    }
  }
}
