using Godot;
using Taiju.Objects.Reversible.Godot;

namespace Taiju.Scenes;

public partial class Spawner : ReversibleSpawner {
  private void PlayRush(PackedScene packedScene) {
    var scene = packedScene.Instantiate<Node3D>();
    scene.QueueFree();
    var children = scene.GetChildren();
    foreach (var child in children) {
      scene.RemoveChild(child);
      Field.AddChild(child);
    }
  }
}
