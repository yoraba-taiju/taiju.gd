using System;
using System.Diagnostics.Contracts;
using Godot;
using Vector3 = System.Numerics.Vector3;

namespace Taiju.Objects.BulletServer; 

public interface IBullet {
  public struct Attitude {
    public Vector2 Position;
    public Vector2 Angle;
  }
  [Pure]
  public Attitude AttitudeAt(double integrateTime);
}
