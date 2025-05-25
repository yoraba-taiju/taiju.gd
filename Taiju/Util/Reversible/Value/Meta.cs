namespace Taiju.Util.Reversible.Value;

class Meta<T>(Clock clock, in T initial) : IValue<T>
  where T : struct {
  private T value_ = initial;
  public uint HistoryBegin => clock.CurrentTick;
  public ref readonly T Ref => ref value_;
  public ref T Mut => ref value_;
}
