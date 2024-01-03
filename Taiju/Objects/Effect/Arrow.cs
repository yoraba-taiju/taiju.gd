#nullable enable
using System;
using System.Linq;
using Godot;
using Taiju.Objects.Enemy;
using Taiju.Objects.Reversible.Godot;
using Taiju.Objects.Reversible.Value;
using Taiju.Objects.Witch;
using Taiju.Util.Godot;

namespace Taiju.Objects.Effect;

public partial class Arrow : ReversibleTubeTrail<Arrow.Param> {
  private Node3D? enemies_;
  private Sora? sora_;
  [Export] private Color arrayColor_ = Godot.Colors.PaleVioletRed;
  [Export] private bool randomizedHue_ = true;
  [Export] public double TrackPeriod = 0.5;
  [Export] public double StopPeriod = 0.05;
  [Export] private float maxRotateAngle_ = 30.0f;
  [Export] public Vector3 InitialPosition { get; set; }
  [Export] public Vector3 InitialVelocity { get; set; }
  public struct Param {
  }
  private Dense<Record> record_;
  public enum State {
    Tracking,
    Stop,
  }
  public struct Record {
    public State State;
    public Vector3 Position;
    public Vector3 Velocity;
    public EnemyBase? Target;
  }

  public override void _Ready() {
    base._Ready();
    { // ColorSetting
      arrayColor_.ToHsv(out _, out var saturation, out var value);
      var hue = Random.Shared.NextSingle();
      Colors = new Color[Length];
      for (var i = 0; i < Length; ++i) {
        var f = (float)i;
        var alpha = 1.0f - f / (Length - 1);
        Colors[i] = Color.FromHsv(hue, saturation, value, alpha * alpha * alpha);
      }
    }
    enemies_ = GetNode<Node3D>("/root/Root/Field/Enemy")!;
    sora_ = GetNode<Sora>("/root/Root/Field/Witch/Sora")!;
    record_ = new Dense<Record>(Clock, new Record {
      State = State.Tracking,
      Position = InitialPosition,
      Velocity = InitialVelocity,
      Target = FindEnemy(),
    });
  }

  private EnemyBase? FindEnemy() {
    var sora = sora_!;
    var enemies = enemies_!;
    var soraPosition = sora.Position;
    var max = enemies.GetChildCount();
    EnemyBase? nearest = null;
    var distance = float.PositiveInfinity;
    for (var i = 0; i < max; ++i) {
      var enemy = enemies.GetChildOrNull<EnemyBase>(i);
      if (enemy is not { IsAlive: true }) {
        continue;
      }
      var pos = enemy.Position;
      if (Mathf.Abs(pos.X) > 25f || Mathf.Abs(pos.Y) > 13.5f) {
        continue;
      }
      var d = (pos - soraPosition).Length();
      if (d > distance) {
        continue;
      }
      nearest = enemy;
      distance = d;
    }
    return nearest;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    base._ProcessForward(integrateTime, dt);
    ref var rec = ref record_.Mut;
    var target = rec.Target;
    var leftPeriod = TrackPeriod - integrateTime;
    switch (rec.State) {
      case State.Tracking: {
        if (target is not { IsAlive: true }) {
          if (rec.Velocity.Length() < 120.0f) {
            rec.Velocity = rec.Velocity.Normalized() * 120.0f;
          }
          rec.Position += rec.Velocity * (float)dt;
          Push(rec.Position, new Param());
          if (Mathf.Abs(rec.Position.X) >= 30.0f || Mathf.Abs(rec.Position.Y) >= 18.0f) {
            Destroy();
          }
          break;
        }
        if (Mathf.Abs(target.Position.X) > 25f || Mathf.Abs(target.Position.Y) > 13.5f) {
          rec.Target = null;
          break;
        }
        if (leftPeriod < 0.001f ) {
          Push(target.Position, new Param());
          target.Hit();
          rec.Target = null;
          rec.State = State.Stop;
          break;
        }
        if (leftPeriod < TrackPeriod / 3.0f) {
          var direction = target.Position - rec.Position;
          Mover.Follow(direction, rec.Velocity, (float)(maxRotateAngle_ * dt));
          rec.Position += rec.Velocity * (float)dt;
          Push(rec.Position, new Param());
          break;
        }
        var force = Mover.TrackingForce(
          rec.Position,
          rec.Velocity,
          target.Position,
          target.LinearVelocity,
          (float)leftPeriod
        );
        rec.Velocity += force * (float)dt;
        rec.Position += rec.Velocity * (float)dt;
        Push(rec.Position, new Param());
      }
        break;

      case State.Stop: {
        if (integrateTime > (TrackPeriod + StopPeriod)) {
          Destroy();
        }
      }
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
    return true;
  }
}
