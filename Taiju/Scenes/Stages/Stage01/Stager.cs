using Taiju.Scenes.Model.Events;
using Taiju.UI.HUD;

namespace Taiju.Scenes.Stages.Stage01;

public partial class Stager : Stages.Stager {
  private bool initialized_;
  public override void _Ready() {
    base._Ready();
    LoadStage("res://Scenes/Stages/Stage01/Stage.tscn.json");
    initialized_ = false;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    base._ProcessForward(integrateTime, dt);
    if (!initialized_) {
      Sora.AbsorbMagicElement(SpellGauge.MaxItems / 2);
      initialized_ = true;
    }
    return true;
  }

  protected override void OnTrigger(double stagePosition, Trigger trigger) {
    throw new System.NotImplementedException();
  }
}
