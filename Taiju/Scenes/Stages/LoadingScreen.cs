using Godot;

namespace Taiju.Scenes.Stages;

public partial class LoadingScreen : Node2D {
  private StageLoader stageLoader_;
  private ProgressBar progressBar_;

  public override void _Ready() {
    base._Ready();
    stageLoader_ = GetNode<StageLoader>("/root/Root/StageLoader")!;
    progressBar_ = GetNode<ProgressBar>("/root/Root/ProgressBar")!;
  }

  public override void _Process(double delta) {
    base._Process(delta);
    progressBar_.Value = stageLoader_.Progress;
    if (stageLoader_.Done) {
      var root = GetTree().GetRoot();
      var scene = stageLoader_.StageScene.Instantiate<Node3D>()!;
      var stager = scene.GetNode<Stager>("Stager")!;
      stager.Stage = stageLoader_.Stage.ForStager();
      stager.ResourceManager = stageLoader_.ResourceManager;
      root.RemoveChild(this);
      QueueFree();
      root.AddChild(scene);
    }
  }
}
