using Taiju.Reversible;
using Taiju.Reversible.Value;

namespace TaijuTest.Reversible.Value; 

public class SparseValueTest : ValueTest<Sparse<int>> {
  protected override Sparse<int> Create(Clock clock, int initial) {
    return new Sparse<int>(clock, initial);
  }
}
