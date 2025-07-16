using Godot;
using Taiju.UI.HUD;

namespace Taiju.Scenes.Stages.Stage01;

public partial class Stager : Stages.Stager {
  private bool initialized_ = false;
  private Model.Stage stage_;
  public override void _Ready() {
    base._Ready();
    stage_ = Loader.StageLoader.Load("res://Scenes/Stages/Stage01/Stage.tscn.json");
    GD.Print(stage_.Events.Length);
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    if (!initialized_) {
      Sora.AbsorbMagicElement(SpellGauge.MaxItems / 2);
      initialized_ = true;
    }
    switch (integrateTime) {

    }
    return base._ProcessForward(integrateTime, dt);
  }
}
