using Godot;
using Taiju.Objects.Enemy;
using Taiju.Util.Reversible.Godot;

namespace Taiju.Objects.Rush;

public partial class Rush : ReversibleNode3D {
  [Export] private double intervalToCheckAlive_ = 0.1;
  private double timeToCheckAlive_;
  public override bool _ProcessForward(double integrateTime, double dt) {
    timeToCheckAlive_ -= dt;
    if (timeToCheckAlive_ < 0) {
      timeToCheckAlive_ += intervalToCheckAlive_;
      if (!IsRushAlive) {
        OnDestroy();
        Destroy();
      }
    }
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    timeToCheckAlive_ = 0;
    return true;
  }

  private bool IsRushAlive {
    get {
      if (GetChildCount() <= 0) {
        return false;
      }
      foreach (var node in GetChildren()) {
        if (node is not EnemyBase enemy) {
          GD.PrintErr($"Unknown node: {node.Name}/{node} (class = {node.GetClass()})");
          continue;
        }
        if (enemy.IsAlive) {
          return true;
        }
      }
      return false;
    }
  }

  protected void OnDestroy() {
    
  }
}
