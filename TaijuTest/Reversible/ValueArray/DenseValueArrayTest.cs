namespace TaijuTest.Reversible.ValueArray; 

using Taiju.Objects.Reversible;
using Taiju.Objects.Reversible.ValueArray;

public class DenseValueArrayTest : AbstractValueArrayTest<DenseArray<int>> {
  protected override DenseArray<int> Create(Clock clock, int initial) {
    return new DenseArray<int>(clock, 2, initial);
  }
}
