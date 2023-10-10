namespace TaijuTest.Reversible.ValueArray; 

using Taiju.Reversible;
using Taiju.Reversible.ValueArray;

public class DenseValueArrayTest : AbstractValueArrayTest<DenseArray<int>> {
  protected override DenseArray<int> Create(Clock clock, int initial) {
    return new DenseArray<int>(clock, 2, initial);
  }
}
