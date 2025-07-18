using Godot;

namespace Taiju.Scenes.Stages;

public partial class LoadingScreen : Node2D {
  private StageLoader stageLoader_;

  public override void _Ready() {
    base._Ready();
    stageLoader_ = GetNode<StageLoader>("/root/Root/StageLoader")!;
  }

  public override void _Process(double delta) {
    base._Process(delta);
    if (stageLoader_.Done) {
      var root = GetTree().GetRoot();
      var scene = ResourceLoader.Load<PackedScene>(stageLoader_.StageScenePath).Instantiate<Node3D>()!;
      var stager = scene.GetNode<Stager>("Stager")!;
      stager.Stage = stageLoader_.Stage;
      root.RemoveChild(this);
      QueueFree();
      root.AddChild(scene);
    }
  }
}
