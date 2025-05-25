#nullable enable
using System;

namespace Taiju.Util.Reversible.Value;

public struct Dense<T> : IValue<T> where T : struct {
  private readonly Clock clock_;
  private readonly T[] entries_;
  public uint HistoryBegin { get; private set; }
  private uint lastTouchedLeap_;
  private uint lastTouchedTick_;
  private uint maxLeap_;
  private uint maxTick_;

  public delegate void ClonerFn(ref T dst, in T src);

  private readonly ClonerFn? clonerFn_;

  public Dense(Clock clock, in T initial) : this(clock, null, initial) {
  }

  public Dense(Clock clock, ClonerFn? clonerFn, in T initial) {
    clock_ = clock;
    entries_ = new T[Clock.HistoryLength];
    HistoryBegin = clock.CurrentTick;
    entries_[HistoryBegin % Clock.HistoryLength] = initial;
    lastTouchedLeap_ = clock.CurrentLeap;
    lastTouchedTick_ = clock.CurrentTick;
    maxLeap_ = clock.CurrentLeap;
    maxTick_ = clock.CurrentTick;
    clonerFn_ = clonerFn;
  }

  private void Debug() {
    var vs = "[";
    for (var i = HistoryBegin; i <= lastTouchedTick_; i++) {
      vs += $"{i}: {entries_[i % Clock.HistoryLength]}";
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

  public ref readonly T Ref {
    get {
      var currentTick = clock_.CurrentTick;
      if (currentTick < HistoryBegin) {
        Debug();
        throw new InvalidOperationException("Can't access before value born.");
      }

      if (clock_.CurrentLeap != lastTouchedLeap_) {
        lastTouchedTick_ = clock_.AdjustTick(lastTouchedLeap_, currentTick);
        lastTouchedLeap_ = clock_.CurrentLeap;
        return ref entries_[lastTouchedTick_ % Clock.HistoryLength];
      }

      if (currentTick != lastTouchedTick_) {
        lastTouchedTick_ = Math.Min(currentTick, lastTouchedTick_);
      }

      return ref entries_[lastTouchedTick_ % Clock.HistoryLength];
    }
  }

  public ref T Mut {
    get {
      var currentTick = clock_.CurrentTick;
      if (currentTick < HistoryBegin) {
        Debug();
        throw new InvalidOperationException("Can't access before value born.");
      }

      if (clock_.CurrentLeap != lastTouchedLeap_) {
        var branch = clock_.BranchTickOfLeap(lastTouchedLeap_);
        var v = entries_[branch % Clock.HistoryLength];
        for (var i = branch + 1; i <= currentTick; i++) {
          if (clonerFn_ == null) {
            entries_[i % Clock.HistoryLength] = v;
          } else {
            clonerFn_(ref entries_[i % Clock.HistoryLength], in v);
          }
        }

        lastTouchedLeap_ = clock_.CurrentLeap;
        lastTouchedTick_ = currentTick;
        HistoryBegin = Math.Max(HistoryBegin,
          (currentTick >= Clock.HistoryLength) ? currentTick - Clock.HistoryLength + 1 : 0);
      } else if (lastTouchedTick_ != currentTick) {
        var v = entries_[lastTouchedTick_ % Clock.HistoryLength];
        for (var i = lastTouchedTick_; i <= currentTick; i++) {
          if (clonerFn_ == null) {
            entries_[i % Clock.HistoryLength] = v;
          } else {
            clonerFn_(ref entries_[i % Clock.HistoryLength], in v);
          }
        }

        lastTouchedTick_ = currentTick;
        HistoryBegin = Math.Max(HistoryBegin,
          (currentTick >= Clock.HistoryLength) ? currentTick - Clock.HistoryLength + 1 : 0);
      }
      maxLeap_ = clock_.CurrentLeap;
      maxTick_ = currentTick;
      return ref entries_[currentTick % Clock.HistoryLength];
    }
  }

  public DenseClone<T> Clone() {
    var entries = new T[maxTick_ - HistoryBegin];
    if ((maxTick_ % Clock.HistoryLength) <= (HistoryBegin % Clock.HistoryLength)) {
      // またいでる
      var begA = HistoryBegin % Clock.HistoryLength;
      var spanA = new ReadOnlySpan<T>(entries_, (int)begA, (int)(Clock.HistoryLength - begA));
      var spanB = new ReadOnlySpan<T>(entries_, 0, (int)(maxTick_ % Clock.HistoryLength));
      if (spanA.Length + spanB.Length != maxTick_ - HistoryBegin) {
        throw new InvalidOperationException("[BUG]");
      }
      spanA.CopyTo(new Span<T>(entries, 0, spanA.Length));
      spanB.CopyTo(new Span<T>(entries, spanA.Length,spanB.Length));
    } else {
      var spanA = new ReadOnlySpan<T>(entries_, (int)(HistoryBegin % Clock.HistoryLength), (int)(maxTick_ - HistoryBegin));
      spanA.CopyTo(new Span<T>(entries, 0, spanA.Length));
    }
    return new DenseClone<T>(clock_, HistoryBegin, entries);
  }
}
