namespace Taiju.Tests.Util.Godot;

using GdUnit4;
using global::Godot;
using Taiju.Util;
using static GdUnit4.Assertions;

// RandomNumberGenerator は Godot のネイティブクラスなのでエンジンが要る。
[TestSuite]
[RequireGodotRuntime]
public class VecTest {
  [TestCase]
  public void BasicTest() {
    var rng = new RandomNumberGenerator();
    var v = Vec.RandomAngle(rng);
    AssertThat(v.IsNormalized()).IsTrue();
  }
}
