using System.Diagnostics.Contracts;
using Godot;
using Vector3 = System.Numerics.Vector3;

namespace Taiju.Objects.BulletServer.Bullets; 

public readonly struct Linear: IBullet {
  private readonly Vector2 spawnPosition_;
  private readonly Vector2 velocity_;
  public Linear(Vector2 spawnAt, Vector2 initialSpeed) {
    spawnPosition_ = spawnAt;
    velocity_ = initialSpeed;
  }
  public Vector2 PositionAt(double integrateTime) {
    return spawnPosition_ + velocity_ * (float)integrateTime;
  }
}
