using Godot;
using Taiju.Util.Godot;

namespace TaijuTest.Util.Godot;

public class VecTest {
  [Test]
  public void BasicTest() {
    var rng = new RandomNumberGenerator();
    var v = Vec.RandomAngle(rng);
    Assert.That(v.IsNormalized());
  }
}
