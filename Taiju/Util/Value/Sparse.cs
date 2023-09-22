using System;

namespace Taiju.Util.Value;

public struct Sparse<T> : IValue<T> where T : struct {
  private struct Entry {
    public uint tick;
    public T value;
  }

  private readonly Clock clock_;
  private uint lastTouchedLeap_;
  private uint lastTouchedTick_;

  private readonly Entry[] entries_;
  private uint entriesBeg_;
  private uint entriesLen_;

  public delegate void ClonerFn(ref T dst, in T src);

  private readonly ClonerFn? clonerFn_;

  public Sparse(Clock clock, in T initial) : this(clock, null, initial) {
  }

  public Sparse(Clock clock, ClonerFn? clonerImpl, in T initial) {
    clock_ = clock;
    entries_ = new Entry[Clock.HistoryLength];
    entries_[0] = new Entry {
      tick = clock.CurrentTick,
      value = initial,
    };
    lastTouchedLeap_ = clock.CurrentLeap;
    lastTouchedTick_ = clock.CurrentTick;
    entriesBeg_ = 0;
    entriesLen_ = 1;
    clonerFn_ = clonerImpl;
  }

  private void Debug() {
    var vs = "[";
    for (var i = 0; i < entriesLen_; i++) {
      ref readonly var e = ref entries_[(i + entriesBeg_) % Clock.HistoryLength];
      vs += $"[{i}]({e.tick}, {e.value})";
      if (i < entriesLen_ - 1) {
        vs += ", ";
      }
    }

    vs += "]";
    System.Diagnostics.Debug.WriteLine($"Current: {clock_.CurrentTick} / lastTouched: ({lastTouchedLeap_}, {lastTouchedTick_})");
    System.Diagnostics.Debug.WriteLine($"Record: {vs}");
  }

  private readonly uint LowerBound(uint beg, uint end, uint tick) {
    while (beg < end) {
      // tick[beg] < tick <= tick[end]
      var midIdx = beg + (end - beg) / 2;
      var midTick = entries_[midIdx % Clock.HistoryLength].tick;
      if (tick == midTick) {
        return midIdx;
      }

      if (midTick < tick) {
        beg = midIdx + 1;
      }
      else {
        // tick < midTick
        end = midIdx;
      }
    }

    return beg;
  }

  private readonly uint UpperBound(uint beg, uint end, uint tick) {
    while (beg < end) {
      // tick[beg] <= tick < tick[end]
      var midIdx = beg + (end - beg) / 2;
      if (beg == midIdx) {
        return beg;
      }

      var midTick = entries_[midIdx % Clock.HistoryLength].tick;
      if (tick == midTick) {
        return midIdx;
      }

      if (midTick < tick) {
        beg = midIdx;
      }
      else {
        // tick < midTick
        end = midIdx - 1;
      }
    }

    return end;
  }

  public ref readonly T Ref {
    get {
      var currentTick = clock_.CurrentTick;
      var currentLeap = clock_.CurrentLeap;
      if (currentLeap == lastTouchedLeap_ && lastTouchedTick_ <= currentTick) {
        return ref entries_[(entriesBeg_ + entriesLen_ - 1) % Clock.HistoryLength].value;
      }

      var tick = clock_.AdjustTick(lastTouchedLeap_, currentTick);
      var rawIdx = UpperBound(
        entriesBeg_,
        entriesBeg_ + entriesLen_,
        tick
      );
      var idx = rawIdx % Clock.HistoryLength;
      if (currentTick < entries_[idx].tick) {
        Debug();
        throw new InvalidOperationException("Can't access before value born.");
      }

      var currentLen = rawIdx - entriesBeg_ + 1;
      ref var e = ref entries_[idx];
      entriesLen_ = Math.Min(currentLen, entriesLen_);
      lastTouchedLeap_ = currentLeap;
      lastTouchedTick_ = e.tick;
      return ref e.value;
    }
  }

  public ref T Mut {
    get {
      var currentTick = clock_.CurrentTick;
      var currentLeap = clock_.CurrentLeap;
      if (currentLeap == lastTouchedLeap_ && currentTick == lastTouchedTick_) {
        return ref entries_[(entriesBeg_ + entriesLen_ - 1) % Clock.HistoryLength].value;
      }

      var tick = clock_.AdjustTick(lastTouchedLeap_, currentTick);
      var rawIdx = LowerBound(
        entriesBeg_,
        entriesBeg_ + entriesLen_,
        tick
      );
      //UnityEngine.Debug.Log($"{rawIdx}");
      var oldEntriesLen = entriesLen_;
      var idx = rawIdx % Clock.HistoryLength;
      if (idx == entriesBeg_) {
        if (entriesLen_ == Clock.HistoryLength) {
          entriesBeg_ = (entriesBeg_ + 1) % Clock.HistoryLength;
        }
        else {
          if (currentTick < entries_[idx].tick) {
            Debug();
            throw new InvalidOperationException("Can't access before value born.");
          }

          entriesLen_ = (rawIdx - entriesBeg_) + 1;
        }
      }
      else {
        entriesLen_ = (rawIdx - entriesBeg_) + 1;
      }

      entries_[idx].tick = currentTick;
      if (oldEntriesLen < entriesLen_) {
        if (clonerFn_ == null) {
          entries_[idx].value = entries_[(idx + Clock.HistoryLength - 1) % Clock.HistoryLength].value;
        }
        else {
          clonerFn_(
            ref entries_[idx].value,
            in entries_[(idx + Clock.HistoryLength - 1) % Clock.HistoryLength].value
          );
        }
      }

      lastTouchedLeap_ = currentLeap;
      lastTouchedTick_ = currentTick;
      return ref entries_[idx].value;
    }
  }
}
