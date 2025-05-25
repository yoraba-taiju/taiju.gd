using System;
using Godot;

namespace Taiju.UI.HUD;

public partial class SpellGaugeSlot : Sprite2D {
  // Constants
  public const int ItemPerSlot = 16;
  [Export(PropertyHint.Range, "0,1.0,0.1")] private double initialTickDuration_ = 0.5;

  // Status
  private int numElements_;
  private double tickDuration_;
  
  /// <summary>
  /// Set # of items, and update UI.
  /// </summary>
  /// <param name="numElements"># of items</param>
  /// <returns>Change whether the state of this slot is changed or not.</returns>
  public bool SetNumItems(int numElements) {
    var oldNumElements = numElements_;
    numElements_ = Math.Clamp(numElements, 0, ItemPerSlot);
    if (numElements_ == oldNumElements) {
      return false;
    }
    var isEmpty = numElements_ <= 0;
    var isFull = numElements_ >= ItemPerSlot;
    var x = 
      isEmpty ? 256.0 :
      isFull ? 128.0 :
      128.0 + (128.0 * (1.0 - (double)numElements_ / ItemPerSlot));
    SetRegionPosition(x);
    return isFull; // numElements_ is changed, checked by the initial if guarded return.
  }

  private void SetRegionPosition(double x) {
    var rect = RegionRect;
    var pos = rect.Position;
    pos.X = (float)x;
    rect.Position = pos;
    RegionRect = rect;
  }

  public override void _Ready() {
    base._Ready();
    numElements_ = 0;
    tickDuration_ = initialTickDuration_;
  }

  public override void _Process(double dt) {
    base._Process(dt);
    if (numElements_ >= ItemPerSlot) {
      // Full animation.
      SetRegionPosition(tickDuration_ <= initialTickDuration_ / 2.0 ? 0.0 : 128.0);
      tickDuration_ -= dt;
      if (tickDuration_ <= 0) {
        tickDuration_ += initialTickDuration_;
      }
    }
  }
  public void ResetTiming() {
    tickDuration_ = initialTickDuration_;
  }
}
