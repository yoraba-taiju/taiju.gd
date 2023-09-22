using Godot;

namespace Taiju.Objects.Enemy.Drone0;

public partial class Drone0 : Node3D {
  private Node3D sora_;
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    var player = GetNode<AnimationPlayer>("AnimationPlayer");
    var anim = player.GetAnimation("Rotate");
    anim.LoopMode = Animation.LoopModeEnum.Linear;
    anim.RemoveTrack(anim.GetTrackCount() - 1);
    player.PlaybackActive = true;
    player.Play("Rotate");
    sora_ = GetNode<Node3D>("/root/Root/Field/Witch/Sora");
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta) {
  }
}
