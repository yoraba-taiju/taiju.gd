namespace TaijuTest.Reversible.Value; 

using Taiju.Reversible;
using Taiju.Reversible.Value;

public class DenseValueTest : AbstractValueTest<Dense<int>> {
  protected override Dense<int> Create(Clock clock, int initial) {
    return new Dense<int>(clock, initial);
  }
}
