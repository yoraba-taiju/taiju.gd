namespace Taiju.Tests.Util.Reversible.ValueArray;

using System;
using Taiju.Util.Reversible;
using Taiju.Util.Reversible.ValueArray;
using static GdUnit4.Assertions;

/**
 * DenseArray<int> / SparseArray<int> に共通の性質。
 *
 * gdUnit4 は基底クラスから継承した [TestCase] を discover しないので、
 * ここには実体だけを置き、[TestCase] は派生の [TestSuite] 側で並べる。
 */
public abstract class ValueArrayTestBase<T>
  where T : IValueArray<int> {
  protected abstract T Create(Clock clock, int initial);

  protected void BasicTestImpl() {
    var clock = new Clock();
    var v = Create(clock, 0);

    AssertThat(v.Ref.Length).IsEqual(2);
    AssertThat(v.Ref[0]).IsEqual(0);
    AssertThat(v.Ref[0]).IsEqual(0);
    v.Mut[1] = 1;
    AssertThat(v.Ref[1]).IsEqual(1);
    clock.Tick();
    AssertThat(v.Ref[1]).IsEqual(1);
    v.Mut[0] = 2;
    AssertThat(v.Ref[0]).IsEqual(2);
    clock.Back();
    AssertThat(v.Ref[1]).IsEqual(1);
    clock.Leap();
    clock.Tick();
    AssertThat(v.Ref[1]).IsEqual(1);
    v.Mut[0] = 3;
    AssertThat(v.Ref[0]).IsEqual(3);
  }

  protected void CantBeAccessedBeforeImpl() {
    var clock = new Clock();
    clock.Tick();
    var v = Create(clock, 0);
    clock.Back();
    clock.Leap();
    AssertThrown(() => { v.Mut[1] = 10; })
      .IsInstanceOf<InvalidOperationException>();
  }

  protected void LongTestImpl() {
    var clock = new Clock();
    clock.Tick();
    clock.Tick();
    clock.Tick();
    var v = Create(clock, 0);
    for (var i = 0; i < Clock.HistoryLength * 2; ++i) {
      clock.Tick();
      AssertThat(v.Ref[0]).IsEqual(i);
      AssertThat(v.Mut[0]).IsEqual(i);
      v.Mut[0] = i + 1;
    }

    var backCount = 0;
    for (var i = (int)Clock.HistoryLength * 2 - 1; i >= Clock.HistoryLength; --i) {
      AssertThat(v.Ref[0]).IsEqual(i + 1);
      clock.Back();
      backCount++;
    }

    AssertThat(backCount).IsEqual((int)Clock.HistoryLength);
  }

  protected void InvalidOperationImpl() {
    var clock = new Clock();
    clock.Tick();
    var w = Create(clock, 1);
    clock.Back();
    AssertThrown(() => {
      var unused = w.Ref.Length;
    }).IsInstanceOf<InvalidOperationException>();
  }

  protected void BackAndRefImpl() {
    var clock = new Clock();
    var v = Create(clock, 0);
    // tick = 0
    clock.Tick();
    // tick = 1
    v.Mut[0] = 1;
    clock.Tick();
    // tick = 2
    clock.Tick();
    // tick = 3
    v.Mut[1] = 3;
    clock.Back();
    // tick = 2
    clock.Leap();
    AssertThat(v.Ref[0]).IsEqual(1);
    AssertThat(v.Ref[1]).IsEqual(0);
  }

  protected void LastRefImpl() {
    var clock = new Clock();
    var v = Create(clock, 0);
    // tick = 0
    clock.Tick();
    // tick = 1
    v.Mut[0] = 1;
    clock.Tick();
    // tick = 2
    clock.Tick();
    // tick = 3,
    v.Mut[1] = 3;
    clock.Back();
    // tick = 2
    clock.Leap();
    clock.Tick();
    // tick = 3
    clock.Tick();
    // tick = 4
    AssertThat(v.Ref[0]).IsEqual(1);
    AssertThat(v.Ref[1]).IsEqual(0);
  }
}
