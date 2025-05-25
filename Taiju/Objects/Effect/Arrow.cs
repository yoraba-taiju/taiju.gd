#nullable enable
using System;
using Godot;
using Taiju.Objects.Enemy;
using Taiju.Objects.Witch;
using Taiju.Util;
using Taiju.Util.Reversible.Godot;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Effect;

public partial class Arrow : ReversibleTubeTrail<Arrow.Param> {
  public const float DefaultSpeed = 120.0f;
  private const float DefaultReTrackSpeed = 180.0f;
  [Export(PropertyHint.Range, "0,8,1")] private int damage_ = 2;
  private Node3D? enemyField_;
  private Sora? sora_;
  [Export] private Color arrayColor_ = Colors.PaleVioletRed;
  [Export] private bool randomizedHue_ = true;
  [Export(PropertyHint.Range, "0,1,0.01")] private double trackPeriod_ = 0.5;
  [Export(PropertyHint.Range, "0,1,0.01")] private double stopPeriod_ = 0.05;
  [Export(PropertyHint.Range, "0,180,0.1")] private float maxRotateAngle_ = 180.0f;
  [Export] public Vector3 InitialPosition;
  [Export] public Vector3 InitialVelocity;

  public struct Param;
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
    public double TrackPeriod;
  }

  public override void _Ready() {
    { // ColorSetting
      arrayColor_.ToHsv(out _, out var saturation, out var value);
      var hue = Random.Shared.NextSingle();
      TubeColors = new Color[Length];
      for (var i = 0; i < Length; ++i) {
        var f = (float)i;
        var alpha = 1.0f - f / (Length - 1);
        TubeColors[i] = Color.FromHsv(hue, saturation, value, alpha * alpha * alpha);
      }
      var material = (StandardMaterial3D)Material;
      material.Emission = Color.FromHsv(hue, saturation, value);
    }


    // TubeColors should be initialized before this call.
    base._Ready();

    enemyField_ = GetNode<Node3D>("/root/Root/Field/Enemy")!;
    sora_ = GetNode<Sora>("/root/Root/Field/Witch/Sora")!;
    record_ = new Dense<Record>(Clock, new Record {
      State = State.Tracking,
      Position = InitialPosition,
      Velocity = InitialVelocity,
      Target = FindEnemy(),
      TrackPeriod = trackPeriod_,
    });
  }

  private EnemyBase? FindEnemy() {
    var enemyField = enemyField_!;
    var sora = sora_!;
    var soraPosition = sora.Position;
    var distance = float.PositiveInfinity;
    EnemyBase? nearest = null;
    foreach (var rush in enemyField.GetChildren()) {
      if (rush == null) {
        continue;
      }
      if (rush is not Node3D rushNode) {
        GD.PrintErr($"Unknown rush: {rush.Name}/{rush} (class = {rush.GetClass()})");
        continue;
      }
      foreach (var node in rushNode.GetChildren()) {
        if (node == null) {
          continue;
        }
        if (node is not EnemyBase enemy) {
          GD.PrintErr($"Unknown enemy: {node.Name}/{node} (class = {node.GetClass()})");
          continue;
        }
        UpdateNearest(enemy);
      }
    }
    return nearest;

    void UpdateNearest(EnemyBase enemy) {
      if (!enemy.IsAlive) {
        return;
      }
      var position = enemy.Position;
      if (Mathf.Abs(position.X) > 25f || Mathf.Abs(position.Y) > 13.5f) {
        return;
      }
      var d = (position - soraPosition).Length();
      if (d > distance) {
        return;
      }
      nearest = enemy;
      distance = d;
    }
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    base._ProcessForward(integrateTime, dt);
    ref var rec = ref record_.Mut;
    var target = rec.Target;
    var leftPeriod = rec.TrackPeriod - integrateTime;
    switch (rec.State) {
      case State.Tracking: {
        if (target is not { IsAlive: true }) {
          if (rec.Velocity.Length() < DefaultSpeed) {
            rec.Velocity = rec.Velocity.Normalized() * DefaultSpeed;
          }
          rec.Position += rec.Velocity * (float)dt;
          Push(rec.Position, new Param());
          if (Mathf.Abs(rec.Position.X) >= 30.0f || Mathf.Abs(rec.Position.Y) >= 18.0f) {
            Destroy();
          }
          if (leftPeriod >= 0.1) {
            var nextTarget = FindEnemy();
            if (nextTarget != null) {
              rec.Target = nextTarget;
              rec.Velocity = rec.Velocity.Normalized() * DefaultReTrackSpeed;
              rec.TrackPeriod = integrateTime + trackPeriod_;
            }
          }
          break;
        }
        if (Mathf.Abs(target.Position.X) > 25f || Mathf.Abs(target.Position.Y) > 13.5f) {
          rec.Target = null;
          break;
        }
        if (leftPeriod < 1.0 / 120.0) {
          Push(target.Position, new Param());
          target.Hit(damage_);
          rec.Target = null;
          rec.State = State.Stop;
          break;
        }
        if (leftPeriod < trackPeriod_ / 3.0f) {
          var direction = target.Position - rec.Position;
          Mover.Follow(direction, rec.Velocity, (float)Mathf.DegToRad(maxRotateAngle_ * dt));
          rec.Position += rec.Velocity * (float)dt;
          Push(rec.Position, new Param());
          break;
        }
        { // Interval point
          var force = Mover.TrackingForce(
            rec.Position,
            rec.Velocity,
            target.Position,
            target.LinearVelocity,
            leftPeriod - (dt/2)
          );
          rec.Velocity += force * (float)dt/2;
          rec.Position += rec.Velocity * (float)dt/2;
          Push(rec.Position, new Param());
        }
        { // Current point
          var force = Mover.TrackingForce(
            rec.Position,
            rec.Velocity,
            target.Position,
            target.LinearVelocity,
            leftPeriod
          );
          rec.Velocity += force * (float)dt;
          rec.Position += rec.Velocity * (float)dt;
          Push(rec.Position, new Param());
        }
      }
        break;

      case State.Stop: {
        if (integrateTime > (rec.TrackPeriod + stopPeriod_)) {
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
