using Godot;

namespace Taiju.Util.Godot;

public static class Mover {
  public static Vector3 Follow(in Vector3 delta, in Vector3 velocity) {
    return delta.Normalized() * velocity.Length();
  }

  public static Vector3 Follow(Vector3 targetDirection, in Vector3 currentVelocity, float maxAngle) {
    var speed = currentVelocity.Length();
    var direct = targetDirection.Normalized() * speed;
    var dx = currentVelocity.X;
    var dy = currentVelocity.Y;
    var c = Mathf.Cos(maxAngle);
    var s = Mathf.Sin(maxAngle);
    var limited1 = new Vector3(dx * c - s * dy, dx * s + dy * c, 0.0f);
    if (currentVelocity.Dot(direct) >= currentVelocity.Dot(limited1)) {
      return direct;
    }

    s = -s;
    var limited2 = new Vector3(dx * c - s * dy, dx * s + dy * c, 0.0f);
    return targetDirection.Dot(limited1) >= targetDirection.Dot(limited2) ? limited1 : limited2;
  }
  
  public static Vector3 TrackingForce(in Vector3 fromPos, in Vector3 fromVel, in Vector3 toPos, in Vector3 toVel, float leftPeriod) {
    return 2.0f * ((toPos - fromPos) + ((toVel - fromVel) * leftPeriod)) / (leftPeriod * leftPeriod);
  }
}
