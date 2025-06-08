using System;
using Godot;

namespace Taiju.UI.HUD;

public partial class Score : Node2D {
  [Export] private double rate_ = 3.0;
  private long score_;
  private long scoreToDisplay_;
  private Label counterLabel_;

  public override void _Ready() {
    base._Ready();
    counterLabel_ = GetNode<Label>("Counter")!;
  }

  public override void _Process(double delta) {
    base._Process(delta);
    if (score_ == scoreToDisplay_) {
      return;
    }
    var scoreDelta = score_ - scoreToDisplay_;
    var add = (long)(scoreDelta * rate_ * delta);
    if (scoreDelta <= 100) {
      scoreToDisplay_ = score_;
    } else {
      scoreToDisplay_ += add;
    }
    SetText(scoreToDisplay_);
  }

  public void Set(long score) {
    score_ = score;
  }

  public void Add(long scoreDelta) {
    score_ += scoreDelta;
  }

  private void SetText(long score) {
    counterLabel_.Text = score.ToString().PadLeft(10, '0');
  }
}
