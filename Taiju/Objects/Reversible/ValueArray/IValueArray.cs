using System;

namespace Taiju.Objects.Reversible.ValueArray; 

public interface IValueArray<T> where T : struct {
  public ReadOnlySpan<T> Ref { get; }

  public Span<T> Mut { get; }
}
