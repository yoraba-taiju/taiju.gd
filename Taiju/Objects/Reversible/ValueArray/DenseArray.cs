using System;

namespace Taiju.Objects.Reversible.ValueArray;

public struct DenseArray<T> : IValueArray<T> where T : struct {
  private readonly Clock clock_;
  private readonly int size_;
  private readonly T[] entries_;
  private uint historyBegin_;
  private uint lastTouchedLeap_;
  private uint lastTouchedTick_;

  public DenseArray(Clock clock, uint size,in T initial) {
    clock_ = clock;
    size_ = (int)size;
    entries_ = new T[Clock.HistoryLength * size];
    historyBegin_ = clock.CurrentTick;
    entries_[historyBegin_ % Clock.HistoryLength] = initial;
    lastTouchedLeap_ = clock.CurrentLeap;
    lastTouchedTick_ = clock.CurrentTick;
  }

  private void Debug() {
    var vs = "[";
    for (var i = historyBegin_; i <= lastTouchedTick_; i++) {
      var span = new Span<T>(entries_, (int)(i % Clock.HistoryLength) * size_, size_);
      vs += $"{i}: {span.ToString()}";
      if (i != lastTouchedTick_) {
        vs += ", ";
      }
    }

    vs += "]";

    var msg =
      $"Current: {clock_.CurrentTick} / Beg: {historyBegin_}, lastTouched: ({lastTouchedLeap_}, {lastTouchedTick_})\n" +
      $"Record: {vs}";

    Console.WriteLine(msg);
    //Godot.GD.PrintErr(msg);
  }

  public ReadOnlySpan<T> Ref {
    get {
      var currentTick = clock_.CurrentTick;
      if (currentTick < historyBegin_) {
        Debug();
        throw new InvalidOperationException("Can't access before value born.");
      }

      if (clock_.CurrentLeap != lastTouchedLeap_) {
        lastTouchedTick_ = clock_.AdjustTick(lastTouchedLeap_, currentTick);
        lastTouchedLeap_ = clock_.CurrentLeap;
        return new ReadOnlySpan<T>(entries_, (int)(lastTouchedTick_ % Clock.HistoryLength) * size_, size_);
      }

      if (currentTick != lastTouchedTick_) {
        lastTouchedTick_ = Math.Min(currentTick, lastTouchedTick_);
      }

      return new ReadOnlySpan<T>(entries_, (int)(lastTouchedTick_ % Clock.HistoryLength) * size_, size_);
    }
  }

  public Span<T> Mut {
    get {
      var currentTick = clock_.CurrentTick;
      if (currentTick < historyBegin_) {
        Debug();
        throw new InvalidOperationException("Can't access before value born.");
      }

      if (clock_.CurrentLeap != lastTouchedLeap_) {
        var branch = clock_.BranchTickOfLeap(lastTouchedLeap_);
        var src = new Span<T>(entries_, (int)(branch % Clock.HistoryLength) * size_, size_);
        for (var i = branch + 1; i <= currentTick; i++) {
          var dst = new Span<T>(entries_, (int)(i % Clock.HistoryLength) * size_, size_);
          src.CopyTo(dst);
        }

        lastTouchedLeap_ = clock_.CurrentLeap;
        lastTouchedTick_ = currentTick;
        historyBegin_ = Math.Max(historyBegin_,
          (currentTick >= Clock.HistoryLength) ? currentTick - Clock.HistoryLength + 1 : 0);
      } else if (lastTouchedTick_ != currentTick) {
        var src = new Span<T>(entries_, (int)(lastTouchedTick_ % Clock.HistoryLength) * size_, size_);
        for (var i = lastTouchedTick_; i <= currentTick; i++) {
          var dst = new Span<T>(entries_, (int)(i % Clock.HistoryLength) * size_, size_);
          src.CopyTo(dst);
        }

        lastTouchedTick_ = currentTick;
        historyBegin_ = Math.Max(historyBegin_,
          (currentTick >= Clock.HistoryLength) ? currentTick - Clock.HistoryLength + 1 : 0);
      }

      return new Span<T>(entries_, (int)(currentTick % Clock.HistoryLength) * size_, size_);
    }
  }
}
