using Taiju.Objects.Reversible;
using Taiju.Objects.Reversible.Value;

namespace TaijuTest.Reversible.Value; 

public class SparseValueTest : AbstractValueTest<Sparse<int>> {
  protected override Sparse<int> Create(Clock clock, int initial) {
    return new Sparse<int>(clock, initial);
  }
}
