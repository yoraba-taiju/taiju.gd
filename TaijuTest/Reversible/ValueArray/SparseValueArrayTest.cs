namespace TaijuTest.Reversible.ValueArray; 

using Taiju.Objects.Reversible;
using Taiju.Objects.Reversible.ValueArray;

public class SparseValueArrayTest : AbstractValueArrayTest<SparseArray<int>> {
  protected override SparseArray<int> Create(Clock clock, int initial) {
    return new SparseArray<int>(clock, 2, initial);
  }
}
