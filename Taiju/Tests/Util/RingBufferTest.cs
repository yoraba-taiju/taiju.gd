namespace Taiju.Tests.Util;

using System;
using GdUnit4;
using Taiju.Util;
using static GdUnit4.Assertions;

[TestSuite]
public class RingBufferTest {
  [TestCase]
  public void BasicTest() {
    var buff = new RingBuffer<int>(8192);
    AssertThat(buff.IsEmpty).IsTrue();
    AssertThat(buff.IsFull).IsFalse();

    buff.AddLast(1);
    AssertThat(buff.IsEmpty).IsFalse();
    AssertThat(buff.IsFull).IsFalse();
    AssertThat(buff.First).IsEqual(1);
    AssertThat(buff.Last).IsEqual(1);

    buff.AddLast(2);
    AssertThat(buff.First).IsEqual(1);
    AssertThat(buff.Last).IsEqual(2);

    buff.AddFirst(0);
    AssertThat(buff.First).IsEqual(0);
    AssertThat(buff[0]).IsEqual(0);
    AssertThat(buff[1]).IsEqual(1);
    AssertThat(buff[2]).IsEqual(2);
  }

  [TestCase]
  public void EmptyTest() {
    var buff = new RingBuffer<int>(1);
    AssertThat(buff.IsFull).IsFalse();
    AssertThat(buff.IsEmpty).IsTrue();

    buff.AddLast(1);
    AssertThat(buff.IsFull).IsTrue();
    AssertThat(buff.IsEmpty).IsFalse();

    AssertThrown(() => { buff.AddLast(2); })
      .IsInstanceOf<InvalidOperationException>();
  }

  [TestCase]
  public void RingTest() {
    var buff = new RingBuffer<int>(256);
    AssertThat(buff.IsFull).IsFalse();
    AssertThat(buff.IsEmpty).IsTrue();
    for (var i = 0; i < buff.Capacity; ++i) {
      buff.AddLast(i);
      AssertThat(buff.Last).IsEqual(i);
      AssertThat(buff.First).IsEqual(0);
    }
    AssertThat(buff.IsFull).IsTrue();
    AssertThat(buff.IsEmpty).IsFalse();
    for (var i = 0; i < buff.Capacity; ++i) {
      var first = buff.RemoveFirst();
      AssertThat(buff.IsFull).IsFalse();
      AssertThat(first).IsEqual(i);
      if (!buff.IsEmpty) {
        AssertThat(buff.First).IsEqual(i + 1);
        AssertThat(buff.Last).IsEqual(buff.Capacity - 1);
      }
    }
    AssertThat(buff.IsFull).IsFalse();
    AssertThat(buff.IsEmpty).IsTrue();
  }

  [TestCase]
  public void LongTest() {
    var buff = new RingBuffer<int>(256);
    AssertThat(buff.IsFull).IsFalse();
    for (var i = 0; i < buff.Capacity; ++i) {
      buff.AddLast(i);
      AssertThat(buff.Last).IsEqual(i);
    }
    AssertThat(buff.IsFull).IsTrue();
    for (var i = 256; i < 8192; ++i) {
      var last = buff.RemoveFirst();
      AssertThat(buff.IsFull).IsFalse();
      AssertThat(last).IsEqual(i - 256);
      buff.AddLast(i);
      AssertThat(buff.IsFull).IsTrue();
    }
    AssertThat(buff.Last).IsEqual(8191);
  }
}
