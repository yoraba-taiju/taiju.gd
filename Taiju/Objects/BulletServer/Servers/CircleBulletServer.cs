using Godot;
using Taiju.Objects.BulletServer.Bullets;

namespace Taiju.Objects.BulletServer.Servers; 

public partial class CircleBulletServer : LinearBulletServer {
  [Export] private double hitSize_ = 0.2;
  private double hitSizeSquared_;

  public override void _Ready() {
    base._Ready();
    hitSizeSquared_ = hitSize_ * hitSize_;
  }

  protected override Response OnBulletMove(IBullet.Attitude attitude) {
    var soraPos = new Vector2(Sora.Position.X, Sora.Position.Y);
    var pos = attitude.Position;

    if ((pos - soraPos).LengthSquared() <= hitSizeSquared_) {
      return Response.HitToSora;
    }

    return Response.None;
  }
}
