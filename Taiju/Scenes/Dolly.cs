using Godot;
using Taiju.Objects.Reversible.Godot;

namespace Taiju.Scenes;

public partial class Dolly : ReversibleAnimationPlayer {
  public override void _Ready() {
    base._Ready();
    PlaybackActive = true;
    Play("dolly");
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    return true;
  }
}
