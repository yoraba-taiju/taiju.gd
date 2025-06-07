using Godot;
using Taiju.Util.Reversible.Godot;

namespace Taiju.Scenes.Stages.Common;

public partial class Cloud : ReversibleNode3D {
  [Export] private double speed_ = 7.0f;
  private float initialX_;
  public override void _Ready() {
    base._Ready();
    initialX_ = Position.X;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    SetCurrentPosition(integrateTime);
    return true;
  }

  public override bool _ProcessLeap(double integrateTime) {
    SetCurrentPosition(integrateTime);
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    SetCurrentPosition(integrateTime);
    return true;
  }

  private void SetCurrentPosition(double integrateTime) {
    var delta = integrateTime * speed_;
    var position = Position;
    position.X = (float)(initialX_ - delta);
    Position = position;
  }
}
