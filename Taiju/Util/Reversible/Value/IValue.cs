namespace Taiju.Util.Reversible.Value;

public interface IValue<T> where T : struct {
  public uint HistoryBegin { get; }
  public ref readonly T Ref { get; }
  public ref T Mut { get; }
}
