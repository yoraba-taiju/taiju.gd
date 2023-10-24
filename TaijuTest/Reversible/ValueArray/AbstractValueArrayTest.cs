namespace TaijuTest.Reversible.ValueArray;

using Taiju.Reversible;
using Taiju.Reversible.ValueArray;

public abstract class AbstractValueArrayTest<T>
  where T : IValueArray<int> {
  protected abstract T Create(Clock clock, int initial);

  [Test]
  public void BasicTest() {
    var clock = new Clock();
    var v = Create(clock, 0);

    Assert.That(v.Ref.Length, Is.EqualTo(2));
    Assert.That(v.Ref[0], Is.EqualTo(0));
    Assert.That(v.Ref[0], Is.EqualTo(0));
    v.Mut[1] = 1;
    Assert.That(v.Ref[1], Is.EqualTo(1));
    clock.Tick();
    Assert.That(v.Ref[1], Is.EqualTo(1));
    v.Mut[0] = 2;
    Assert.That(v.Ref[0], Is.EqualTo(2));
    clock.Back();
    Assert.That(v.Ref[1], Is.EqualTo(1));
    clock.Leap();
    clock.Tick();
    Assert.That(v.Ref[1], Is.EqualTo(1));
    v.Mut[0] = 3;
    Assert.That(v.Ref[0], Is.EqualTo(3));
  }

  [Test]
  public void CantBeAccessedBefore() {
    var clock = new Clock();
    clock.Tick();
    var v = Create(clock, 0);
    clock.Back();
    clock.Leap();
    Assert.Throws<InvalidOperationException>(() => { v.Mut[1] = 10; });
  }

  [Test]
  public void LongTest() {
    var clock = new Clock();
    clock.Tick();
    clock.Tick();
    clock.Tick();
    var v = Create(clock, 0);
    for (var i = 0; i < Clock.HistoryLength * 2; ++i) {
      clock.Tick();
      v.Mut[0] = i;
    }

    var backCount = 0;
    for (var i = Clock.HistoryLength * 2 - 1; i >= Clock.HistoryLength; --i) {
      var j = (int)i;
      Assert.That(v.Ref[0], Is.EqualTo(i));
      clock.Back();
      backCount++;
    }

    Assert.That(backCount, Is.EqualTo(Clock.HistoryLength));
  }

  [Test]
  public void InvalidOperation() {
    var clock = new Clock();
    clock.Tick();
    var w = Create(clock, 1);
    clock.Back();
    Assert.Throws<InvalidOperationException>(() => {
      var unused = w.Ref;
    });
  }

  [Test]
  public void BackAndRef() {
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
    Assert.That(v.Ref[0], Is.EqualTo(1));
    Assert.That(v.Ref[1], Is.EqualTo(0));
  }

  [Test]
  public void LastRef() {
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
    Assert.That(v.Ref[0], Is.EqualTo(1));
    Assert.That(v.Ref[1], Is.EqualTo(0));
  }
}
