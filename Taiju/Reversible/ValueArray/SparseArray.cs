using System;

namespace Taiju.Reversible.ValueArray;

public struct SparseArray<T> : IValueArray<T> where T : struct {
  private readonly Clock clock_;
  private uint lastTouchedLeap_;
  private uint lastTouchedTick_;

  private readonly T[] storage_;
  private readonly uint[] ticks_;
  private uint entriesBeg_;
  private uint entriesLen_;

  private readonly int size_;

  public SparseArray(Clock clock, uint size, in T initial) {
    clock_ = clock;
    storage_ = new T[Clock.HistoryLength * size];
    ticks_ = new uint[Clock.HistoryLength];
    size_ = (int)size;
    ticks_[0] = clock.CurrentTick;
    Array.Fill(storage_, initial, 0, (int)size);
    lastTouchedLeap_ = clock.CurrentLeap;
    lastTouchedTick_ = clock.CurrentTick;
    entriesBeg_ = 0;
    entriesLen_ = 1;
  }

  private void Debug() {
    var vs = "[";
    for (var i = 0; i < entriesLen_; i++) {
      var tick = ticks_[(i + entriesBeg_) % Clock.HistoryLength];
      var span = new ReadOnlySpan<T>(storage_, (int)((i + entriesBeg_) % Clock.HistoryLength) * size_, size_);
      vs += $"[{i}]({tick}, {span.ToString()})";
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
      var midTick = ticks_[midIdx % Clock.HistoryLength];
      if (tick == midTick) {
        return midIdx;
      }

      if (midTick < tick) {
        beg = midIdx + 1;
      } else {
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

      var midTick = ticks_[midIdx % Clock.HistoryLength];
      if (tick == midTick) {
        return midIdx;
      }

      if (midTick < tick) {
        beg = midIdx;
      } else {
        // tick < midTick
        end = midIdx - 1;
      }
    }

    return end;
  }

  public ReadOnlySpan<T> Ref {
    get {
      var currentTick = clock_.CurrentTick;
      var currentLeap = clock_.CurrentLeap;
      if (currentLeap == lastTouchedLeap_ && lastTouchedTick_ <= currentTick) {
        return new ReadOnlySpan<T>(storage_, (int)((entriesBeg_ + entriesLen_ - 1) % Clock.HistoryLength) * size_,
          size_);
      }

      var tick = clock_.AdjustTick(lastTouchedLeap_, currentTick);
      var rawIdx = UpperBound(
        entriesBeg_,
        entriesBeg_ + entriesLen_,
        tick
      );
      var idx = rawIdx % Clock.HistoryLength;
      if (currentTick < ticks_[idx]) {
        Debug();
        throw new InvalidOperationException("Can't access before value born.");
      }

      var currentLen = rawIdx - entriesBeg_ + 1;
      var lastTouchedTick = ticks_[idx];
      entriesLen_ = Math.Min(currentLen, entriesLen_);
      lastTouchedLeap_ = currentLeap;
      lastTouchedTick_ = lastTouchedTick;
      return new ReadOnlySpan<T>(storage_, (int)idx * size_, size_);
    }
  }

  public Span<T> Mut {
    get {
      var currentTick = clock_.CurrentTick;
      var currentLeap = clock_.CurrentLeap;
      if (currentLeap == lastTouchedLeap_ && currentTick == lastTouchedTick_) {
        return new Span<T>(storage_, (int)((entriesBeg_ + entriesLen_ - 1) % Clock.HistoryLength) * size_, size_);
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
        } else {
          if (currentTick < ticks_[idx]) {
            Debug();
            throw new InvalidOperationException("Can't access before value born.");
          }

          entriesLen_ = (rawIdx - entriesBeg_) + 1;
        }
      } else {
        entriesLen_ = (rawIdx - entriesBeg_) + 1;
      }

      ticks_[idx] = currentTick;
      if (oldEntriesLen < entriesLen_) {
        Array.Copy(
          storage_,
          (int)((idx + Clock.HistoryLength - 1) % Clock.HistoryLength) * size_,
          storage_,
          idx * size_,
          size_
        );
      }

      lastTouchedLeap_ = currentLeap;
      lastTouchedTick_ = currentTick;
      return new Span<T>(storage_, (int)idx * size_, size_);
    }
  }
}
