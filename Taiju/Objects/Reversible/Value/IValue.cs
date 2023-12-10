namespace Taiju.Objects.Reversible.Value; 

public interface IValue<T> where T : struct {
  public ref readonly T Ref { get; }

  public ref T Mut { get; }
}
