using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace Taiju.Tests.Harness;

/**
 * ハーネス自身の検査。合成検体 (Probes.cs) を使って、
 * ReversibilityHarness が緑にすべきものを緑に、赤にすべきものを赤にすることを確かめる。
 *
 * これが通っていれば、本物の敵で赤が出たときに「ハーネスのバグ」を疑わなくて済む。
 */
[TestSuite]
public class HarnessSelfTest {
  private const uint ForwardTicks = 120;
  private const uint BackTicks = 60;

  // leap の先で回す分。forward は back より 1 tick 多く要る。
  private const uint LeapForwardTicks = 40;
  private const uint LeapBackTicks = 30;

  private static Task<ReversibilityHarness> BootWithAsync<T>() where T : Node3D, new() =>
    ReversibilityHarness.BootAsync(scene =>
      scene.GetNode<Node3D>("Field/Enemy/DefaultRush").AddChild(new T { Name = typeof(T).Name }));

  [TestCase]
  [RequireGodotRuntime]
  public async Task FunctionProbeRoundTrips() {
    using var harness = await BootWithAsync<FunctionProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    harness.AssertRoundTrip(BackTicks);
  }

  [TestCase]
  [RequireGodotRuntime]
  public async Task RecordingProbeRoundTrips() {
    using var harness = await BootWithAsync<RecordingProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    harness.AssertRoundTrip(BackTicks);
  }

  /**
   * leap を跨いだ往復性。Dense は leap が変わると Mut で BranchTickOfLeap からの埋め戻し、
   * Ref で AdjustTick に入るので、leap を跨がないとこの経路がまるごと素通りになる。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task RecordingProbeRoundTripsAcrossLeap() {
    using var harness = await BootWithAsync<RecordingProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    await harness.LeapAsync();

    AssertThat(harness.CurrentLeap).IsEqual(1u);

    await harness.ForwardAsync(LeapForwardTicks);
    await harness.BackAsync(LeapBackTicks);
    // back 2 回ぶん + leap の瞬間の 1 回。
    harness.AssertRoundTrip(BackTicks + 1 + LeapBackTicks);
  }

  [TestCase]
  [RequireGodotRuntime]
  public async Task FunctionProbeRoundTripsAcrossLeap() {
    using var harness = await BootWithAsync<FunctionProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    await harness.LeapAsync();
    await harness.ForwardAsync(LeapForwardTicks);
    await harness.BackAsync(LeapBackTicks);
    harness.AssertRoundTrip(BackTicks + 1 + LeapBackTicks);
  }

  /**
   * 壊れた検体をちゃんと赤にできること。
   * これが通らないなら、他のテストの緑は「何も見ていない」ことの証明でしかない。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task LeakyProbeIsReportedAsBroken() {
    using var harness = await BootWithAsync<LeakyProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);

    AssertThat(harness.ObservedMotion).IsTrue();
    AssertThat(harness.DiffReport).IsNotEmpty();
  }

  /**
   * 静止物しか居ないときは「往復性を検査できていない」と判定すること。
   * 差分は 0 件 (往復性自体は自明に成り立つ) だが、それを緑と呼んではいけない。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task StationaryProbeIsNotMistakenForSuccess() {
    using var harness = await BootWithAsync<StationaryProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);

    AssertThat(harness.DiffReport).IsEqual("");
    AssertThat(harness.ObservedMotion).IsFalse();
  }

  /**
   * 巻き戻しが実際に起きていること。空振りしていないことの土台。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task BackPhaseActuallyRewinds() {
    using var harness = await BootWithAsync<FunctionProbe>();
    await harness.ForwardAsync(ForwardTicks);
    var peak = harness.CurrentTick;

    await harness.BackAsync(BackTicks);

    AssertThat(harness.CurrentTick).IsEqual(peak - BackTicks);
    AssertThat(harness.ComparedCount).IsGreaterEqual((int)BackTicks);
  }

  /**
   * leap は tick を進めない。分岐した瞬間は分岐元と同じ時刻に居る。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task LeapBranchesWithoutMovingTheClock() {
    using var harness = await BootWithAsync<RecordingProbe>();
    await harness.ForwardAsync(ForwardTicks);
    await harness.BackAsync(BackTicks);
    var branchTick = harness.CurrentTick;

    await harness.LeapAsync();

    AssertThat(harness.CurrentLeap).IsEqual(1u);
    AssertThat(harness.CurrentTick).IsEqual(branchTick);
    AssertThat(harness.DiffReport).IsEqual("");
  }

  /**
   * 巻き戻せる範囲 (255 tick) を超える forward は、差分の解釈が不能になるので拒否すること。
   */
  [TestCase]
  [RequireGodotRuntime]
  public async Task ForwardBeyondTheWindowIsRefused() {
    using var harness = await BootWithAsync<FunctionProbe>();
    (await AssertThrown(harness.ForwardAsync(ReversibilityHarness.MaxForwardTicks + 1)))
      .IsInstanceOf<System.ArgumentOutOfRangeException>();
  }
}
