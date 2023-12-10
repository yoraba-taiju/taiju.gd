using System;

namespace Taiju.Objects.Reversible;

public class Clock {
  public const uint HistoryLength = 128;

  /* current leaps */
  public uint CurrentLeap { get; private set; }
  public uint CurrentTick { get; private set; }

  /* History management */
  private readonly uint[] historyBranches_ = new uint[HistoryLength];
  private uint HistoryBegin { get; set; }
  private uint HistoryEnd => CurrentTick;

  public Clock() {
    historyBranches_[0] = uint.MaxValue;
    CurrentLeap = 0;
    CurrentTick = 0;
    HistoryBegin = 0;
  }
  
  public void Tick() {
    CurrentTick++;
    HistoryBegin = Math.Max(HistoryBegin, (CurrentTick >= HistoryLength) ? (CurrentTick - HistoryLength + 1) : 0);
  }

  public void Back() {
    if (CurrentTick > HistoryBegin) {
      CurrentTick--;
    }
  }

  public void Leap() {
    for (var i = (CurrentLeap >= HistoryLength) ? (CurrentLeap - HistoryLength) : 0; i <= CurrentLeap; ++i) {
      var idx = i % HistoryLength;
      historyBranches_[idx] = Math.Min(historyBranches_[idx], CurrentTick);
    }

    CurrentLeap++;
    historyBranches_[CurrentLeap % HistoryLength] = uint.MaxValue;
    // DebugBranch();
  }
  
  private void DebugBranch() {
    var branches = "";
    for (var i = (CurrentLeap >= HistoryLength) ? (CurrentLeap - HistoryLength) : 0; i <= CurrentLeap; ++i) {
      branches += $"(i={historyBranches_[i % HistoryLength]}), ";
    }
    Console.WriteLine($"Leaping {CurrentLeap} at {CurrentTick}. branches: [{branches}]");
  }
  
  public uint AdjustTick(uint lastTouchLeap, uint tick) {
    return Math.Min(historyBranches_[lastTouchLeap % HistoryLength], tick);
  }

  public uint BranchTickOfLeap(uint leap) {
    return historyBranches_[leap % HistoryLength];
  }
}
