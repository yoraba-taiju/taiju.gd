namespace TaijuTest.Util;

using Taiju.Util;

public class RingBufferTest {
  [Test]
  public void BasicTest() {
    var buff = new RingBuffer<int>(8192);
    Assert.IsTrue(buff.IsEmpty);
    Assert.IsFalse(buff.IsFull);
    buff.AddLast(1);
    Assert.IsFalse(buff.IsEmpty);
    Assert.IsFalse(buff.IsFull);
    Assert.That(buff.First, Is.EqualTo(1));
    Assert.That(buff.Last, Is.EqualTo(1));
    buff.AddLast(2);
    Assert.That(buff.First, Is.EqualTo(1));
    Assert.That(buff.Last, Is.EqualTo(2));
    buff.AddFirst(0);
    Assert.That(buff.First, Is.EqualTo(0));
      
    Assert.That(buff[0], Is.EqualTo(0));
    Assert.That(buff[1], Is.EqualTo(1));
    Assert.That(buff[2], Is.EqualTo(2));
  }
  [Test]
  public void EmptyTest() {
    var buff = new RingBuffer<int>(1);
    Assert.IsFalse(buff.IsFull);
    buff.AddLast(1);
    Assert.IsTrue(buff.IsFull);
    Assert.Throws<InvalidOperationException>(() => {
      buff.AddLast(2);
    });
  }
  [Test]
  public void LongTest() {
    var buff = new RingBuffer<int>(256);
    Assert.IsFalse(buff.IsFull);
    for (var i = 0; i < buff.Capacity; ++i) {
      buff.AddLast(i);
    }
    Assert.IsTrue(buff.IsFull);
    for (var i = 256; i < 8192; ++i) {
      var last = buff.RemoveFirst();
      Assert.IsFalse(buff.IsFull);
      Assert.That(last, Is.EqualTo(i - 256));
      buff.AddLast(i);
      Assert.IsTrue(buff.IsFull);
    }
    Assert.That(buff.Last, Is.EqualTo(8191));
  }
}
