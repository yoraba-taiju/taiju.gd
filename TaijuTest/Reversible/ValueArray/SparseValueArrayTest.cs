namespace TaijuTest.Reversible.ValueArray; 

using Taiju.Reversible;
using Taiju.Reversible.ValueArray;

public class SparseValueArrayTest : AbstractValueArrayTest<SparseArray<int>> {
  protected override SparseArray<int> Create(Clock clock, int initial) {
    return new SparseArray<int>(clock, 2, initial);
  }
}
