using System.Diagnostics.Contracts;
using Godot;
using Vector3 = System.Numerics.Vector3;

namespace Taiju.Objects.BulletServer; 

public interface IBullet {
  [Pure]
  public Vector2 PositionAt(double integrateTime);
}
