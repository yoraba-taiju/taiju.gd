using System;

namespace Taiju.Util.Reversible.ValueArray;

public readonly struct MetaArray<T>(Clock clock, uint size,in T initial) : IValueArray<T>
where T : struct
{
  public uint HistoryBegin => clock.CurrentTick;

  private readonly T[] values_ = new T[size];
  public ReadOnlySpan<T> Ref => new ReadOnlySpan<T>(values_);
  public Span<T> Mut => new(values_);
}
