namespace TaijuTest.Reversible.Value; 

using Taiju.Objects.Reversible;
using Taiju.Objects.Reversible.Value;

public class DenseValueTest : AbstractValueTest<Dense<int>> {
  protected override Dense<int> Create(Clock clock, int initial) {
    return new Dense<int>(clock, initial);
  }
}
