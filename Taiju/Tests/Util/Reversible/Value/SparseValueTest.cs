namespace Taiju.Tests.Util.Reversible.Value;

using GdUnit4;
using Taiju.Util.Reversible;
using Taiju.Util.Reversible.Value;

[TestSuite]
public class SparseValueTest : ValueTestBase<Sparse<int>> {
  protected override Sparse<int> Create(Clock clock, int initial) => new(clock, initial);

  [TestCase] public void BasicTest() => BasicTestImpl();
  [TestCase] public void CantBeAccessedBefore() => CantBeAccessedBeforeImpl();
  [TestCase] public void LongTest() => LongTestImpl();
  [TestCase] public void InvalidOperation() => InvalidOperationImpl();
  [TestCase] public void BackAndRef() => BackAndRefImpl();
  [TestCase] public void LastRef() => LastRefImpl();
}
