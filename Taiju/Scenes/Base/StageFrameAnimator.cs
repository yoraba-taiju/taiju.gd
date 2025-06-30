using Godot;
using Taiju.Util.Reversible.Godot;

namespace Taiju.Scenes.Base;

// FIXME: Really hacky class!
[Tool]
public partial class StageFrameAnimator : ReversibleAnimationPlayer {
  private bool inGame_ = true;
  [Export] public bool HideOnRun = true;
  private bool started_;
  private Node2D frame_;
  public override void _Ready() {
    inGame_ = !Engine.IsEditorHint();
    if (inGame_) {
      base._Ready();
      frame_ = GetNode<Node2D>("StageFrame")!;
      if (HideOnRun) {
        frame_.Visible = false;
        var parent = GetParentOrNull<Node2D>();
        if (parent != null) {
          parent.Visible = false;
        }
      }
      Play("Stage");
      started_ = true;
    } else {
      frame_ = GetNode<Node2D>("StageFrame")!;
      CurrentAnimationChanged += OnCurrentAnimationChanged;
    }
  }
  private void OnCurrentAnimationChanged(string name) {
    started_ = true;
  }
  private void SetAnimationPosition(double currentTime) {
    frame_.Position = new Vector2((float)(100 * currentTime), 0);
  }

  private void SetAnimationPosition() {
    if (started_) {
      SetAnimationPosition(CurrentAnimationPosition);
    }
  }

  public override void _Process(double delta) {
    base._Process(delta);
    if (!inGame_) {
      SetAnimationPosition();
    }
  }
  public override bool _ProcessForward(double integrateTime, double dt) {
    SetAnimationPosition();
    return true;
  }
  public override bool _ProcessBack(double integrateTime) {
    if (started_) {
      Seek(integrateTime, true);
      SetAnimationPosition(integrateTime);
    }
    return true;
  }
}
