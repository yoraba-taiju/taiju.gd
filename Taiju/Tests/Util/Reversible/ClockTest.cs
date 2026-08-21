namespace Taiju.Tests.Util.Reversible;

using GdUnit4;
using Taiju.Util.Reversible;
using Taiju.Util.Reversible.Value;
using static GdUnit4.Assertions;

/**
 * 巻き戻し機構の非自明な性質。
 * 詳しい根拠は Util/Reversible/CLAUDE.md を参照。
 */
[TestSuite]
public class ClockTest {
  /**
   * Back() で到達できるのはちょうど HistoryLength - 1 歩。
   * 256 スロットのリングが保持できるのは 256 個の相異なる tick なので、
   * 最新 T から戻れるのは T-255 まで。256 歩目は T と同じスロットに落ちる。
   */
  [TestCase]
  public void BackReachesExactlyHistoryLengthMinusOne() {
    var clock = new Clock();
    for (var i = 0; i < 1000; ++i) {
      clock.Tick();
    }

    var steps = 0u;
    while (clock.Back()) {
      steps++;
    }

    AssertThat(steps).IsEqual(Clock.HistoryLength - 1);
    AssertThat(clock.CurrentTick).IsEqual(1000u - (Clock.HistoryLength - 1));
  }

  /**
   * 分岐点より過去へ再分岐すると、古い leap の有効期限も引き下げられる。
   * historyBranches_ は単調非増加で、更新が Math.Min になっている理由。
   */
  [TestCase]
  public void ReBranchingLowersExpiryOfOlderLeaps() {
    var clock = new Clock();
    for (var i = 0; i < 100; ++i) {
      clock.Tick();
    }
    // leap 0 はまだ分岐していないのでどこまでも有効。
    AssertThat(clock.BranchTickOfLeap(0)).IsEqual(uint.MaxValue);

    for (var i = 0; i < 40; ++i) {
      clock.Back();
    }
    clock.Leap(); // leap 1 @ tick 60
    AssertThat(clock.BranchTickOfLeap(0)).IsEqual(60u);
    AssertThat(clock.BranchTickOfLeap(1)).IsEqual(uint.MaxValue);

    for (var i = 0; i < 30; ++i) {
      clock.Tick();
    }
    for (var i = 0; i < 50; ++i) {
      clock.Back();
    }
    clock.Leap(); // leap 2 @ tick 40

    // 60 だった leap 0 の有効期限が 40 まで下がる。leap 1 も同様。
    AssertThat(clock.BranchTickOfLeap(0)).IsEqual(40u);
    AssertThat(clock.BranchTickOfLeap(1)).IsEqual(40u);
    AssertThat(clock.BranchTickOfLeap(2)).IsEqual(uint.MaxValue);
  }

  /**
   * 上の引き下げが値の読み取りに効いていること。
   * branches[0] が 60 のままだと「捨てた未来」の値が返ってしまう。
   */
  [TestCase]
  public void ValueIsFrozenAtBranchTickAfterReBranching() {
    var clock = new Clock();
    var x = new Dense<int>(clock, 0);
    for (var i = 0; i < 100; ++i) {
      clock.Tick();
      x.Mut = (int)clock.CurrentTick;
    }

    for (var i = 0; i < 40; ++i) {
      clock.Back();
    }
    clock.Leap(); // leap 1 @ tick 60
    for (var i = 0; i < 30; ++i) {
      clock.Tick(); // leap 1 では何も書かない
    }

    for (var i = 0; i < 50; ++i) {
      clock.Back();
    }
    clock.Leap(); // leap 2 @ tick 40
    for (var i = 0; i < 10; ++i) {
      clock.Tick(); // tick 50
    }

    // 分岐点 tick 40 の値で凍結される。50 が返ったら引き下げが効いていない。
    AssertThat(x.Ref).IsEqual(40);
  }

  /**
   * leap をまたいだ最初の Mut は、分岐点の値で間のスロットを埋め戻す。
   * 埋めていないと巻き戻したときに前の時間線の残骸が見える。
   */
  [TestCase]
  public void MutAcrossLeapFillsSlotsWithBranchValue() {
    var clock = new Clock();
    var x = new Dense<int>(clock, 0);
    for (var i = 0; i < 100; ++i) {
      clock.Tick();
      x.Mut = (int)clock.CurrentTick;
    }

    for (var i = 0; i < 60; ++i) {
      clock.Back();
    }
    clock.Leap(); // leap 1 @ tick 40。分岐点の値は 40。

    for (var i = 0; i < 10; ++i) {
      clock.Tick(); // tick 50
    }
    x.Mut = 999; // ここで tick 41..50 が 40 で埋まる
    AssertThat(x.Ref).IsEqual(999);

    for (var i = 0; i < 5; ++i) {
      clock.Back(); // tick 45
    }

    // 埋め戻していなければ leap 0 の残骸 45 が見える。
    AssertThat(x.Ref).IsEqual(40);
  }
}
