namespace TaijuTest.Util;

using Taiju.Util;

public class RingBufferTest {
  [Test]
  public void BasicTest() {
    var buff = new RingBuffer<int>(8192);
    Assert.Multiple(() => {
      Assert.That(buff.IsEmpty, Is.True);
      Assert.That(buff.IsFull, Is.False);
    });
    buff.AddLast(1);
    Assert.Multiple(() => {
      Assert.That(buff.IsEmpty, Is.False);
      Assert.That(buff.IsFull, Is.False);
      Assert.That(buff.First, Is.EqualTo(1));
      Assert.That(buff.Last, Is.EqualTo(1));
    });
    buff.AddLast(2);
    Assert.Multiple(() => {
      Assert.That(buff.First, Is.EqualTo(1));
      Assert.That(buff.Last, Is.EqualTo(2));
    });
    buff.AddFirst(0);
    Assert.Multiple(() => {
      Assert.That(buff.First, Is.EqualTo(0));
      Assert.That(buff[0], Is.EqualTo(0));
      Assert.That(buff[1], Is.EqualTo(1));
      Assert.That(buff[2], Is.EqualTo(2));
    });
    }

    [Test]
  public void EmptyTest() {
    var buff = new RingBuffer<int>(1);
    Assert.Multiple(() => {
      Assert.That(buff.IsFull, Is.False);
      Assert.That(buff.IsEmpty, Is.True);
   });
    buff.AddLast(1);
    Assert.Multiple(() => {
      Assert.That(buff.IsFull, Is.True);
      Assert.That(buff.IsEmpty, Is.False);
    });
    Assert.Throws<InvalidOperationException>(() => {
      buff.AddLast(2);
    });
  }

    [Test]
  public void RingTest() {
    var buff = new RingBuffer<int>(256);
    Assert.That(buff.IsFull, Is.False);
    Assert.That(buff.IsEmpty, Is.True);
    for (var i = 0; i < buff.Capacity; ++i) {
      buff.AddLast(i);
      Assert.That(buff.Last, Is.EqualTo(i));
      Assert.That(buff.First, Is.EqualTo(0));
    }
    Assert.That(buff.IsFull, Is.True);
    Assert.That(buff.IsEmpty, Is.False);
    for (var i = 0; i < buff.Capacity; ++i) {
      var first = buff.RemoveFirst();
      Assert.That(buff.IsFull, Is.False);
      Assert.That(first, Is.EqualTo(i));
      if (!buff.IsEmpty) {
        Assert.That(buff.First, Is.EqualTo(i + 1));
        Assert.That(buff.Last, Is.EqualTo(buff.Capacity - 1));
      }
    }
    Assert.That(buff.IsFull, Is.False);
    Assert.That(buff.IsEmpty, Is.True);
  }
  
  [Test]
  public void LongTest() {
    var buff = new RingBuffer<int>(256);
    Assert.That(buff.IsFull, Is.False);
    for (var i = 0; i < buff.Capacity; ++i) {
      buff.AddLast(i);
      Assert.That(buff.Last, Is.EqualTo(i));
    }
    Assert.That(buff.IsFull, Is.True);
    for (var i = 256; i < 8192; ++i) {
      var last = buff.RemoveFirst();
      Assert.That(buff.IsFull, Is.False);
      Assert.That(last, Is.EqualTo(i - 256));
      buff.AddLast(i);
      Assert.That(buff.IsFull, Is.True);
    }
    Assert.That(buff.Last, Is.EqualTo(8191));
  }
}
