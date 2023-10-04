#nullable enable
using System;

namespace Taiju.Reversible.Value;

public struct Sparse<T> : IValue<T> where T : struct {
  private struct Entry {
    public uint Tick;
    public T Value;
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
      Tick = clock.CurrentTick,
      Value = initial,
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
      vs += $"[{i}]({e.Tick}, {e.Value})";
      if (i < entriesLen_ - 1) {
        vs += ", ";
      }
    }
    vs += "]";

    var msg =
      $"Current: {clock_.CurrentTick} / lastTouched: ({lastTouchedLeap_}, {lastTouchedTick_})" + 
      $"Record: {vs}";
    
    Console.WriteLine(msg);
    //Godot.GD.Print(msg);
  }

  private readonly uint LowerBound(uint beg, uint end, uint tick) {
    while (beg < end) {
      // tick[beg] < tick <= tick[end]
      var midIdx = beg + (end - beg) / 2;
      var midTick = entries_[midIdx % Clock.HistoryLength].Tick;
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

      var midTick = entries_[midIdx % Clock.HistoryLength].Tick;
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
        return ref entries_[(entriesBeg_ + entriesLen_ - 1) % Clock.HistoryLength].Value;
      }

      var tick = clock_.AdjustTick(lastTouchedLeap_, currentTick);
      var rawIdx = UpperBound(
        entriesBeg_,
        entriesBeg_ + entriesLen_,
        tick
      );
      var idx = rawIdx % Clock.HistoryLength;
      if (currentTick < entries_[idx].Tick) {
        Debug();
        throw new InvalidOperationException("Can't access before value born.");
      }

      var currentLen = rawIdx - entriesBeg_ + 1;
      ref var e = ref entries_[idx];
      entriesLen_ = Math.Min(currentLen, entriesLen_);
      lastTouchedLeap_ = currentLeap;
      lastTouchedTick_ = e.Tick;
      return ref e.Value;
    }
  }

  public ref T Mut {
    get {
      var currentTick = clock_.CurrentTick;
      var currentLeap = clock_.CurrentLeap;
      if (currentLeap == lastTouchedLeap_ && currentTick == lastTouchedTick_) {
        return ref entries_[(entriesBeg_ + entriesLen_ - 1) % Clock.HistoryLength].Value;
      }

      var tick = clock_.AdjustTick(lastTouchedLeap_, currentTick);
      var rawIdx = LowerBound(
        entriesBeg_,
        entriesBeg_ + entriesLen_,
        tick
      );
      //Godot.GD.Print($"rawIdx = {rawIdx}");
      var oldEntriesLen = entriesLen_;
      var idx = rawIdx % Clock.HistoryLength;
      if (idx == entriesBeg_) {
        if (entriesLen_ == Clock.HistoryLength) {
          entriesBeg_ = (entriesBeg_ + 1) % Clock.HistoryLength;
        }
        else {
          if (currentTick < entries_[idx].Tick) {
            Debug();
            throw new InvalidOperationException("Can't access before value born.");
          }

          entriesLen_ = (rawIdx - entriesBeg_) + 1;
        }
      }
      else {
        entriesLen_ = (rawIdx - entriesBeg_) + 1;
      }

      entries_[idx].Tick = currentTick;
      if (oldEntriesLen < entriesLen_) {
        if (clonerFn_ == null) {
          entries_[idx].Value = entries_[(idx + Clock.HistoryLength - 1) % Clock.HistoryLength].Value;
        }
        else {
          clonerFn_(
            ref entries_[idx].Value,
            in entries_[(idx + Clock.HistoryLength - 1) % Clock.HistoryLength].Value
          );
        }
      }

      lastTouchedLeap_ = currentLeap;
      lastTouchedTick_ = currentTick;
      return ref entries_[idx].Value;
    }
  }
}
