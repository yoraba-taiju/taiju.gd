using Taiju.UI.HUD;

namespace Taiju.Scenes.Stages.Stage01;

public partial class Stager : Stages.Stager {
  private bool initialized_ = false;
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
