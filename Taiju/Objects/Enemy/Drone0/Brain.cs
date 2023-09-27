using System;
using Godot;
using Taiju.Objects.Witch;
using Taiju.Reversible.Value;
using Taiju.Util.Gd;

namespace Taiju.Objects.Enemy.Drone0;

public partial class Brain : EnemyBase {
  [Export(PropertyHint.Range, "0,180,")] private float maxRotateDegreePerSec_ = 60.0f;
  [Export(PropertyHint.Range, "0,20,")] private float escapeDistance_ = 12.0f;

  //
  private Node3D sora_;

  //
  private enum State {
    Seek,
    Escape,
  };

  private Node3D body_;
  private Dense<Record> record_;

  private record struct Record(
    State State,
    Vector3 Position,
    Vector3 Velocity
  );

  public override void _Ready() {
    base._Ready();
    body_ = GetNode<Node3D>("Body");
    record_ = new Dense<Record>(Clock, new Record(
      State.Seek,
      Vector3.Zero,
      new Vector3(-10.0f, 0.0f, 0.0f)
    ));

    var model = body_.GetNode<Node3D>("Model");
    var player = model.GetNode<AnimationPlayer>("AnimationPlayer");
    var anim = player.GetAnimation("Rotate");
    anim.LoopMode = Animation.LoopModeEnum.Linear;
    anim.RemoveTrack(anim.GetTrackCount() - 1);
    player.PlaybackActive = true;
    player.Play("Rotate");
    sora_ = GetNode<Sora>("/root/Root/Field/Witch/Sora");
  }

  public override void _Process(double dt) {
    var currentPosition = Position;
    var soraPosition = sora_.Position;
    var maxAngle = (float)(dt * maxRotateDegreePerSec_);
    ref var rec = ref record_.Mut;

    switch (rec.State) {
      case State.Seek: {
        var delta = soraPosition - currentPosition;
        if (Mathf.Abs(delta.X) > escapeDistance_) {
          rec.Velocity = Mover.Follow(delta, rec.Velocity, maxAngle);
        }
        else {
          rec.State = State.Escape;
        }
      }
        break;
      case State.Escape: {
        var delta = soraPosition - currentPosition;
        if (delta.Length() < escapeDistance_) {
          var sign = Mathf.Sign(delta.Y);
          if (sign == 0) {
            sign = Math.Sign(Random.Shared.Next());
          }

          rec.Velocity = Vec.Rotate(rec.Velocity, sign * maxAngle) * Mathf.Exp((float)dt / 2);
        }
      }
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }

    rec.Position = Position;
    body_.Rotation = new Vector3(0, 0, Mathf.DegToRad(Vec.Atan2(-rec.Velocity)));
  }

  public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
    ref readonly var rec = ref record_.Ref;
    state.LinearVelocity = rec.Velocity;
  }
}
