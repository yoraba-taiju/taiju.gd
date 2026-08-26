using System.Threading.Tasks;
using GdUnit4;
using Godot;
using Taiju.Objects;
using Taiju.Objects.Witch;
using Taiju.Scenes.Stages;
using Taiju.UI.HUD;
using static GdUnit4.Assertions;

namespace Taiju.Tests.Harness;

/**
 * ハーネスが乗る土台の検査。ここが赤ならハーネスは書けない。
 *
 *   1. ISceneRunner.Load が骨格を /root/Root として生やすこと
 *      (全ノードが "/root/Root/..." の絶対パスで互いを掴んでいるので、
 *       gdUnit4 がラッパを挟んだり改名したりすると何も動かない)
 *   2. SimulateFrames で Clock.CurrentTick が進むこと
 *   3. SimulateActionPress("time_back") で Clock.CurrentTick が戻ること
 *
 * 3 が非自明なのは、project.godot の time_back に**キーボードの割り当てが無い**ため
 * (ジョイパッドのボタン 10 だけ)。gdUnit4 が InputEventAction を投げるなら割り当てを
 * 迂回するので通るが、キーイベントを合成する実装なら通らない。
 */
[TestSuite]
public class SmokeTest {
  private const string ScenePath = "res://Tests/Harness/HarnessStage.tscn";

  private static Node3D Instantiate() {
    var scene = GD.Load<PackedScene>(ScenePath).Instantiate<Node3D>();
    var stager = scene.GetNode<HarnessStager>("Stager");
    // _Ready より前に埋める。理由は HarnessStager のコメント。
    stager.Stage = new Scenes.Model.Stage { Events = [] };
    stager.ResourceManager = new ResourceManager();
    return scene;
  }

  [TestCase]
  [RequireGodotRuntime]
  public async Task SkeletonBootsAtRootRoot() {
    using var runner = ISceneRunner.Load(Instantiate(), true);
    await runner.SimulateFrames(1);

    var tree = runner.Scene().GetTree();
    AssertThat(runner.Scene().GetPath().ToString()).IsEqual("/root/Root");
    AssertThat(tree.Root.GetNodeOrNull<ClockNode>("Root/Clock")).IsNotNull();
    AssertThat(tree.Root.GetNodeOrNull<Player>("Root/Player")).IsNotNull();
    AssertThat(tree.Root.GetNodeOrNull<Node>("Root/Field/HUD/SpellGauge")).IsNotNull();
  }

  [TestCase]
  [RequireGodotRuntime]
  public async Task SimulateFramesAdvancesTheClock() {
    using var runner = ISceneRunner.Load(Instantiate(), true);
    await runner.SimulateFrames(1);
    var clock = runner.Scene().GetNode<ClockNode>("Clock").Clock;

    var before = clock.CurrentTick;
    await runner.SimulateFrames(60, 16);
    AssertThat(clock.CurrentTick).IsGreater(before);
  }

  [TestCase]
  [RequireGodotRuntime]
  public async Task SimulateActionPressDrivesTheClockBack() {
    using var runner = ISceneRunner.Load(Instantiate(), true);
    await runner.SimulateFrames(1);
    var clock = runner.Scene().GetNode<ClockNode>("Clock").Clock;

    await runner.SimulateFrames(60, 16);
    var peak = clock.CurrentTick;
    AssertThat(peak).IsGreater(0u);

    // 魔素が無いと Player は StartBack に入らない。ここで見たいのは入力が届くかどうかだけ。
    runner.Scene().GetNode<Sora>("Field/Witch/Sora").AbsorbMagicElement(SpellGauge.MaxItems);
    runner.SimulateActionPress("time_back");
    await runner.SimulateFrames(30, 16);
    runner.SimulateActionRelease("time_back");

    AssertThat(clock.CurrentTick).IsLess(peak);
  }
}
