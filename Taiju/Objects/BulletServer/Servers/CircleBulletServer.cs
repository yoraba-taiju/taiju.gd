using Godot;
using Taiju.Objects.BulletServer.Bullets;

namespace Taiju.Objects.BulletServer.Servers; 

public partial class CircleBulletServer : LinearBulletServer {
  [Export] private double size_ = 1.0;
  private double sizeSquared_;
  public override void _Ready() {
    base._Ready();
    sizeSquared_ = size_ * size_;
  }

  protected override Response OnBulletMove(IBullet.Attitude attitude) {
    var soraPos = new Vector2(Sora.Position.X, Sora.Position.Y);
    var pos = attitude.Position;

    if ((pos - soraPos).LengthSquared() <= sizeSquared_) {
      return Response.HitToSora;
    }

    return Response.None;
  }
}
