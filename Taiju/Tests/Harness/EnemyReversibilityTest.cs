using System;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using Taiju.Objects.Enemy;

namespace Taiju.Tests.Harness;

/**
 * 実物の敵の往復性。
 *
 * ハーネス自身が正しく動くことは HarnessSelfTest が保証しているので、ここが赤なら
 * 敵の側の記録漏れ・復元漏れを疑う。
 *
 * 検体は状態の持ち方が違うものを選んである:
 *   - Drone0/Straight    … 履歴記録方式 + RigidBody (速度を _IntegrateForces で記録から与える)
 *   - Drone1/FollowCurve … f(t) 方式 (位置が Curve.SampleBaked(integrateTime * speed) だけで決まる)
 *   - Drone4/Kamikaze    … 履歴記録方式 + 状態機械 + 自機追従
 *
 * どれも forward -> back -> leap -> forward -> back まで通す。leap を跨がないと
 * _ProcessLeap と Dense の leap 分岐処理がまるごと素通りになる。
 */
[TestSuite]
public class EnemyReversibilityTest {
  /** 画面 (|X| < 24) の中に留まる位置。外へ出ると EnemyBase が Destroy してしまう。 */
  private static readonly Vector3 SpawnPosition = new(18.0f, 4.0f, 0.0f);

  private static async Task RoundTripAsync(
    string scenePath, uint forwardTicks, uint backTicks, Vector3 spawn, Action<Node3D> configure = null) {
    var leapForwardTicks = backTicks / 2;
    var leapBackTicks = leapForwardTicks - 1;

    using var harness = await ReversibilityHarness.BootAsync(scene => {
      var enemy = GD.Load<PackedScene>(scenePath).Instantiate<Node3D>();
      // Stager.LoadSpawn と同じく、位置は AddChild の前に決める。
      enemy.Position = spawn;
      configure?.Invoke(enemy);
      scene.GetNode<Node3D>("Field/Enemy/DefaultRush").AddChild(enemy);
    });

    await harness.ForwardAsync(forwardTicks);
    await harness.BackAsync(backTicks);
    await harness.LeapAsync();
    await harness.ForwardAsync(leapForwardTicks);
    await harness.BackAsync(leapBackTicks);
    // back 2 回ぶん + leap の瞬間の 1 回。
    harness.AssertRoundTrip(backTicks + 1 + leapBackTicks);
  }

  [TestCase]
  [RequireGodotRuntime]
  public async Task Drone0StraightRoundTrips() =>
    await RoundTripAsync("res://Objects/Enemy/Drone0/Drone0_Straight.tscn", 150, 100, SpawnPosition);

  /**
   * FollowCurve は _Ready で Curve.SampleBaked() を呼ぶので、Curve 抜きではツリーに入れられない。
   * 本番では Stager.LoadSpawn がステージ JSON の曲線を流し込んでいる。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task Drone1FollowCurveRoundTrips() =>
    await RoundTripAsync("res://Objects/Enemy/Drone1/Drone1_FollowCurve.tscn", 150, 100, SpawnPosition, enemy => {
      var curve = new Curve3D();
      curve.AddPoint(new Vector3(20.0f, 6.0f, 0.0f));
      curve.AddPoint(new Vector3(0.0f, -6.0f, 0.0f));
      curve.AddPoint(new Vector3(-20.0f, 6.0f, 0.0f));
      ((IEnemyWithCurve)enemy).Curve = curve;
    });

  /**
   * Kamikaze は自機に向かって突っ込んでくるので、放っておくと窓の中で衝突する。
   * 衝突すると Player が ClockState.OnDamage に入って勝手に巻き戻し始め、往復性どころではなくなる
   * (そうなったら ForwardAsync がその旨を報告して止まる)。
   *
   * 画面の隅から出して窓を短くとってあるのはそのため。この配置での衝突はこの環境で tick 130〜131 で、
   * 窓は 100 tick。遷移が integrateTime 基準なので速いマシンほど衝突 tick は下がりうるが、
   * 詰まったら ForwardAsync が「forward 中に時間が巻き戻った」と言って止まる。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task Drone4KamikazeRoundTrips() =>
    await RoundTripAsync(
      "res://Objects/Enemy/Drone4/Drone4_Kamikaze.tscn", 100, 70, new Vector3(23.0f, 11.0f, 0.0f));
}
