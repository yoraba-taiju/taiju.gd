using System.Linq;
using Godot;
using Taiju.Objects.Reversible.Godot;

namespace Taiju.Objects.Effect;

public partial class Arrow : ReversibleTrail<Arrow.Param> {
  [Export] protected Color ArrayColor = Godot.Colors.DarkRed;
  public struct Param {
  }

  public override void _Ready() {
    base._Ready();
    Colors = new Color[Length];
    for (var i = 0; i < Length; ++i) {
      var f = (float)i;
      Colors[i] = ArrayColor.Darkened(f / Length);
    }
  }
}
