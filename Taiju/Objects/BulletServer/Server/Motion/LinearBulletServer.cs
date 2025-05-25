using Godot;

namespace Taiju.Objects.BulletServer.Server.Motion; 

public abstract partial class LinearBulletServer: BulletServer<Linear> {
  public void SpawnToSora(Vector3 spawnAt, float speed) {
    var velocity = (Sora.Position - spawnAt).Normalized() * speed;
    var bullet = new Linear(new Vector2(spawnAt.X, spawnAt.Y), new Vector2(velocity.X, velocity.Y));
    Spawn(bullet);
  }

  public void Spawn(Vector3 spawnAt, Vector2 velocity) {
    var bullet = new Linear(new Vector2(spawnAt.X, spawnAt.Y), velocity);
    Spawn(bullet);
  }
}
