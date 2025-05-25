using System;

namespace Taiju.Util.Reversible.ValueArray;

public struct DenseArray<T> : IValueArray<T> where T : struct {
  private readonly Clock clock_;
  private readonly int size_;
  private readonly T[] entries_;
  public uint HistoryBegin { get; private set; }
  private uint lastTouchedLeap_;
  private uint lastTouchedTick_;

  public DenseArray(Clock clock, uint size,in T initial) {
    clock_ = clock;
    size_ = (int)size;
    entries_ = new T[Clock.HistoryLength * size];
    HistoryBegin = clock.CurrentTick;
    entries_[HistoryBegin % Clock.HistoryLength] = initial;
    lastTouchedLeap_ = clock.CurrentLeap;
    lastTouchedTick_ = clock.CurrentTick;
  }

  private void Debug() {
    var vs = "[";
    for (var i = HistoryBegin; i <= lastTouchedTick_; i++) {
      var span = new Span<T>(entries_, (int)(i % Clock.HistoryLength) * size_, size_);
      vs += $"{i}: {span.ToString()}";
      if (i != lastTouchedTick_) {
        vs += ", ";
      }
    }

    vs += "]";

    var msg =
      $"Current: {clock_.CurrentTick} / Beg: {HistoryBegin}, lastTouched: ({lastTouchedLeap_}, {lastTouchedTick_})\n" +
      $"Record: {vs}";

    Console.WriteLine(msg);
    //Godot.GD.PrintErr(msg);
  }

  public ReadOnlySpan<T> Ref {
    get {
      var currentTick = clock_.CurrentTick;
      if (currentTick < HistoryBegin) {
        Debug();
        throw new InvalidOperationException("Can't access before value born.");
      }

      if (clock_.CurrentLeap != lastTouchedLeap_) {
        lastTouchedTick_ = clock_.AdjustTick(lastTouchedLeap_, currentTick);
        lastTouchedLeap_ = clock_.CurrentLeap;
        return new ReadOnlySpan<T>(entries_, (int)(lastTouchedTick_ % Clock.HistoryLength) * size_, size_);
      }

      if (currentTick != lastTouchedTick_) {
        lastTouchedTick_ = System.Math.Min(currentTick, lastTouchedTick_);
      }

      return new ReadOnlySpan<T>(entries_, (int)(lastTouchedTick_ % Clock.HistoryLength) * size_, size_);
    }
  }

  public Span<T> Mut {
    get {
      var currentTick = clock_.CurrentTick;
      if (currentTick < HistoryBegin) {
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
        HistoryBegin = Math.Max(HistoryBegin,
          (currentTick >= Clock.HistoryLength) ? currentTick - Clock.HistoryLength + 1 : 0);
      } else if (lastTouchedTick_ != currentTick) {
        var src = new Span<T>(entries_, (int)(lastTouchedTick_ % Clock.HistoryLength) * size_, size_);
        for (var i = lastTouchedTick_; i <= currentTick; i++) {
          var dst = new Span<T>(entries_, (int)(i % Clock.HistoryLength) * size_, size_);
          src.CopyTo(dst);
        }

        lastTouchedTick_ = currentTick;
        HistoryBegin = Math.Max(HistoryBegin,
          (currentTick >= Clock.HistoryLength) ? currentTick - Clock.HistoryLength + 1 : 0);
      }

      return new Span<T>(entries_, (int)(currentTick % Clock.HistoryLength) * size_, size_);
    }
  }
}
