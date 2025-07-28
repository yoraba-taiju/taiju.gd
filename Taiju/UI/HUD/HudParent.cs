using Godot;

namespace Taiju.UI.HUD;

public partial class HudParent : CanvasLayer {
  private Vector2I currentWindowSize_;

  // Nodes & info
  private Node2D scoreNode_;
  private Vector2 originalScoreNodePositionRatio_;
  private SpellGauge spellGauge_;
  private Vector2 originalSpellGaugePositionRatio_;
  public override void _Ready() {
   base._Ready();
   scoreNode_ = GetNode<Node2D>("Score")!;
   var screenWidth = ProjectSettings.GetSetting("display/window/size/viewport_width").AsInt32();
   var screenHeight = ProjectSettings.GetSetting("display/window/size/viewport_height").AsInt32();
   currentWindowSize_ = new Vector2I(screenWidth, screenHeight);
   originalScoreNodePositionRatio_ = scoreNode_.Position / currentWindowSize_;
   spellGauge_ = GetNode<SpellGauge>("SpellGauge")!;
   originalSpellGaugePositionRatio_ = spellGauge_.Position / currentWindowSize_;
  }

  public void OnResize(Vector2I size) {
    if (currentWindowSize_.X == size.X && currentWindowSize_.Y == size.Y) {
      return;
    }
    scoreNode_.Position = size * originalScoreNodePositionRatio_;
    spellGauge_.Position = size * originalSpellGaugePositionRatio_;
  }
}
