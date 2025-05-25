#nullable enable
using System;

namespace Taiju.Util.Reversible.Value;

public readonly struct DenseClone<T> where T : struct {
  private readonly Clock clock_;
  private readonly T[] entries_;
  public uint HistoryEnd { get; }
  public uint HistoryBegin { get; }
  public bool IsAlive => HistoryBegin <= clock_.CurrentTick && clock_.CurrentTick < HistoryEnd;
  public bool IsDead => HistoryEnd <= clock_.CurrentTick;

  internal DenseClone(Clock clock, uint historyBegin, in T[] entries) {
    clock_ = clock;
    entries_ = entries;
    HistoryBegin = historyBegin;
    HistoryEnd = (uint)(historyBegin + entries.Length);
  }

  public ref readonly T Ref {
    get {
      var currentTick = clock_.CurrentTick;
      if (currentTick < HistoryBegin) {
        throw new InvalidOperationException("Can't access before value born.");
      }
      if (HistoryEnd <= currentTick) {
        throw new InvalidOperationException("Can't access after value dead.");
      }
      return ref entries_[currentTick - HistoryBegin];
    }
  }

  public bool HasPrev => HistoryBegin <= clock_.CurrentTick - 1;
  public ref readonly T PrevRef {
    get {
      var currentTick = clock_.CurrentTick - 1;
      if (currentTick < HistoryBegin) {
        throw new InvalidOperationException("Can't access before value born.");
      }
      if (HistoryEnd <= currentTick) {
        throw new InvalidOperationException("Can't access after value dead.");
      }
      return ref entries_[currentTick - HistoryBegin];
    }
  }

  public bool HasNext => clock_.CurrentTick + 1 < HistoryEnd;
  public ref readonly T NextRef {
    get {
      var currentTick = clock_.CurrentTick + 1;
      if (currentTick < HistoryBegin) {
        throw new InvalidOperationException("Can't access before value born.");
      }
      if (HistoryEnd <= currentTick) {
        throw new InvalidOperationException("Can't access after value dead.");
      }
      return ref entries_[currentTick - HistoryBegin];
    }
  }
}
