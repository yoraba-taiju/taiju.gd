using Godot;
using Taiju.Objects.Witch;

namespace Taiju.Objects.BulletServer.Bullets; 

public abstract partial class LinearBulletServer: BulletServer<Linear> {
  public void SpawnToSora(Vector3 spawnAt, double speed) {
    var velocity = (Sora.Position - spawnAt).Normalized() * (float)speed;
    var bullet = new Linear(new Vector2(spawnAt.X, spawnAt.Y), new Vector2(velocity.X, velocity.Y));
    Spawn(bullet);
  }
}
