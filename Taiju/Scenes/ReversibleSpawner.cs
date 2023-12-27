using Godot;
using Taiju.Objects.Reversible.Godot;

namespace Taiju.Scenes;

public partial class Spawner : ReversibleSpawner {
  private void PlayRush(PackedScene packedScene) {
    var scene = packedScene.Instantiate<Node3D>();
    scene.QueueFree();
    var all = scene.GetChildren();
    foreach (var child in all) {
      scene.RemoveChild(child);
      Field.AddChild(child);
    }
  }
}
