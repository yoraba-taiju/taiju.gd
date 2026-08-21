namespace Taiju.Tests.Util.Reversible.ValueArray;

using GdUnit4;
using Taiju.Util.Reversible;
using Taiju.Util.Reversible.ValueArray;

[TestSuite]
public class DenseValueArrayTest : ValueArrayTestBase<DenseArray<int>> {
  protected override DenseArray<int> Create(Clock clock, int initial) => new(clock, 2, initial);

  [TestCase] public void BasicTest() => BasicTestImpl();
  [TestCase] public void CantBeAccessedBefore() => CantBeAccessedBeforeImpl();
  [TestCase] public void LongTest() => LongTestImpl();
  [TestCase] public void InvalidOperation() => InvalidOperationImpl();
  [TestCase] public void BackAndRef() => BackAndRefImpl();
  [TestCase] public void LastRef() => LastRefImpl();
}
