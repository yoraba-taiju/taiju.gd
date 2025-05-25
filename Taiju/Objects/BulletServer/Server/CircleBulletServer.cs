using Godot;
using Taiju.Objects.BulletServer.Server.Motion;

namespace Taiju.Objects.BulletServer.Server; 

public partial class CircleBulletServer : LinearBulletServer {
  [Export(PropertyHint.Range, "0,5,0.1")] private double hitRadius_ = 0.2;
  private double hitRadiusSquared_;

  public override void _Ready() {
    base._Ready();
    hitRadiusSquared_ = hitRadius_ * hitRadius_;
  }

  protected override Response OnBulletMove(IBullet.Attitude attitude) {
    var soraPos = new Vector2(Sora.Position.X, Sora.Position.Y);
    var pos = attitude.Position;

    if ((pos - soraPos).LengthSquared() <= hitRadiusSquared_) {
      return Response.HitToSora;
    }

    return Response.None;
  }
}
