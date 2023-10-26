using Godot;

namespace Taiju.Objects.BulletServer.Bullets; 

public partial class LinearBulletServer: BulletServer<Linear> {
  private double nextIntegrateTime_;
  private RandomNumberGenerator rand_ = new();
}
