using System.Diagnostics.Contracts;
using Godot;

namespace Taiju.Objects.BulletServer.Bullets; 

public readonly struct Linear(Vector2 spawnAt, Vector2 initialSpeed) : IBullet {
  [Pure]
  public IBullet.Attitude AttitudeAt(double integrateTime) {
    return new IBullet.Attitude {
      Position = spawnAt + initialSpeed * (float)integrateTime,
      Angle = initialSpeed,
    };
  }
}
