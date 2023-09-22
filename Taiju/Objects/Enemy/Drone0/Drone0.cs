using System;
using Godot;
using Taiju.Util.Gd;

namespace Taiju.Objects.Enemy.Drone0;

public partial class Drone0 : Node3D {
  [ExportGroup("Behaviours")] [Export(PropertyHint.Range, "0,180,")]
  private float maxRotateDegreePerSec_ = 60.0f;

  //
  private Node3D sora_;

  //
  private enum State{
    Seek,
    Escape,
  };

  private Node3D body_;
  private State state_ = State.Seek;
  private Vector3 velocity_ = new(-10.0f, 0.0f, 0.0f);

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    body_ = GetNode<Node3D>("Body");
    var model = body_.GetNode<Node3D>("Model");
    var player = model.GetNode<AnimationPlayer>("AnimationPlayer");
    var anim = player.GetAnimation("Rotate");
    anim.LoopMode = Animation.LoopModeEnum.Linear;
    anim.RemoveTrack(anim.GetTrackCount() - 1);
    player.PlaybackActive = true;
    player.Play("Rotate");
    sora_ = GetNode<Node3D>("/root/Root/Field/Witch/Sora");
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double dt) {
    var trans = Transform;
    var currentPosition = Position;
    var soraPosition = sora_.Position;
    var maxAngle = (float)(dt * maxRotateDegreePerSec_);

    switch (state_) {
      case State.Seek: {
        var delta = soraPosition - currentPosition;
        if (Mathf.Abs(delta.X) > 10.0f) {
          velocity_ = Mover.Follow(delta, velocity_, maxAngle);
        } else {
          state_ = State.Escape;
        }
      }
        break;
      case State.Escape: {
        var delta = soraPosition - currentPosition;
        if (delta.Length() < 10.0f) {
          var sign = Mathf.Sign(delta.Y);
          if (sign == 0) {
            sign = Math.Sign(Random.Shared.Next());
          }
          velocity_ = Vec.Rotate(velocity_, sign * maxAngle) * Mathf.Exp((float)dt / 2);
        }
      }
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
    Rotation = new Vector3( 0, 0, Mathf.DegToRad(Vec.Atan2(-velocity_)));
    Position += (float)dt * velocity_;
  }
}
