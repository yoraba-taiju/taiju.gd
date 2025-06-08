using System;
using Godot;

namespace Taiju.UI.HUD;

public partial class SpellGauge : Node2D {
  // Constants
  public const int ItemsPerGauge = SpellGaugeSlot.ItemPerSlot * 8;
  private int numElements_;
  private SpellGaugeSlot[] gaugeItems_;
  public override void _Ready() {
    base._Ready();
    gaugeItems_ = [
      GetNode<SpellGaugeSlot>("0")!,
      GetNode<SpellGaugeSlot>("1")!,
      GetNode<SpellGaugeSlot>("2")!,
      GetNode<SpellGaugeSlot>("3")!,
      GetNode<SpellGaugeSlot>("4")!,
      GetNode<SpellGaugeSlot>("5")!,
      GetNode<SpellGaugeSlot>("6")!,
      GetNode<SpellGaugeSlot>("7")!,
    ];
  }
  public void SetGauge(int numElements) {
    var oldNumElements = numElements_;
    numElements_ = Math.Clamp(numElements, 0, SpellGaugeSlot.ItemPerSlot * 8);
    if (numElements_ == oldNumElements) {
      return;
    }
    var full = numElements_ / SpellGaugeSlot.ItemPerSlot;
    var left = numElements_ % SpellGaugeSlot.ItemPerSlot;
    var changed = false;
    for (var i = 0; i < 8; i++) {
      if (i < full) {
        changed = changed || gaugeItems_[i].SetNumItems(SpellGaugeSlot.ItemPerSlot);
      } else {
        gaugeItems_[i].SetNumItems(0);
      }
    }
    if (full < 8) {
      changed = changed || gaugeItems_[full].SetNumItems(left);
    }
    if (changed) {
      foreach (var gauge in gaugeItems_) {
        gauge.ResetTiming();
      }
    }
  }
}
