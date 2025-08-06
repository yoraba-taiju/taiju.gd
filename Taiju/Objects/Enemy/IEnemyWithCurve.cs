using Godot;
using Taiju.Util.Godot;

namespace Taiju.Objects.Enemy;

public interface IEnemyWithCurve {
  public Curve3D Curve { set; }
}
