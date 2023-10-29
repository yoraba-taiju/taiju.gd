using System.Diagnostics.Contracts;
using Godot;

namespace Taiju.Objects.BulletServer.Bullets; 

public readonly struct Linear: IBullet {
  private readonly Vector2 spawnPosition_;
  private readonly Vector2 velocity_;
  public Linear(Vector2 spawnAt, Vector2 initialSpeed) {
    spawnPosition_ = spawnAt;
    velocity_ = initialSpeed;
  }
  public IBullet.Attitude AttitudeAt(double integrateTime) {
    return new IBullet.Attitude {
      Position = spawnPosition_ + velocity_ * (float)integrateTime,
      Angle = velocity_,
    };
  }
}
