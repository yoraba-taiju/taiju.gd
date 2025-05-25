using System;

namespace Taiju.Util.Reversible.ValueArray;

public interface IValueArray<T> where T : struct {
  public uint HistoryBegin { get; }
  public ReadOnlySpan<T> Ref { get; }

  public Span<T> Mut { get; }
}
