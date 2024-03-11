using System.Diagnostics.Contracts;
using Godot;

namespace Taiju.Objects.BulletServer; 

public interface IBullet {
  public struct Attitude {
    public Vector2 Position;
    public Vector2 InitialVelocity;
  }
  [Pure]
  public Attitude AttitudeAt(double integrateTime);
}
