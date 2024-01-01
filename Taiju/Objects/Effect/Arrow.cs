#nullable enable
using System.Linq;
using Godot;
using Taiju.Objects.Enemy;
using Taiju.Objects.Reversible.Godot;
using Taiju.Objects.Reversible.Value;
using Taiju.Objects.Witch;
using Taiju.Util.Godot;

namespace Taiju.Objects.Effect;

public partial class Arrow : ReversibleTrail<Arrow.Param> {
  private Node3D? enemies_;
  private Sora? sora_;
  [Export] protected Color ArrayColor = Godot.Colors.DarkRed;
  [Export] public double Period = 5.0;
  [Export] public Vector3 InitialSpeed { get; set; }
  public struct Param {
  }

  private Dense<Record> state_;
  public struct Record {
    public Vector3 Position;
    public Vector3 Velocity;
    public EnemyBase? Target;
  }

  public override void _Ready() {
    base._Ready();
    Colors = new Color[Length];
    for (var i = 0; i < Length; ++i) {
      var f = (float)i;
      Colors[i] = ArrayColor.Darkened(f / Length);
    }
    enemies_ = GetNode<Node3D>("/root/Root/Field/Enemy")!;
    sora_ = GetNode<Sora>("/root/Root/Field/Witch/Sora")!;
    state_ = new Dense<Record>(Clock, new Record {
      Position = Position,
      Velocity = InitialSpeed,
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
      var d = (enemy.Position - soraPosition).Length();
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
    ref var rec = ref state_.Mut;
    var target = rec.Target;
    if (integrateTime >= Period) {
      target?.Hit();
      rec.Target = null;
      Destroy();
      return true;
    }
    if (target == null) {
      rec.Position += rec.Velocity * (float)dt;
      Push(rec.Position, new Param());
      return true;
    }
    var leftPeriod = Period - integrateTime;
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
    return true;
  }
}
