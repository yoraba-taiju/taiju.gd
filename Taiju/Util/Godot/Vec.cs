using Godot;

namespace Taiju.Util.Gd; 

public static class Vec {
    public static Vector3 Rotate(in Vector3 direction, float angleToRotate) {
        var x = direction.X;
        var y = direction.Y;
        var angle = Mathf.DegToRad(angleToRotate);
        var c = Mathf.Cos(angle);
        var s = Mathf.Sin(angle);
        return new Vector3(x * c - s * y, x * s + y * c, 0);
        //return Quaternion.Euler(0.0f, 0.0f, angleToRotate) * direction;
    }

    public static float Atan2(float x, float y) {
        return Mathf.RadToDeg(Mathf.Atan2(y, x));
    }

    public static float Atan2(in Vector2 v) {
        return Mathf.RadToDeg(Mathf.Atan2(v.Y, v.X));
    }

    public static float Atan2(in Vector3 v) {
        return Mathf.RadToDeg(Mathf.Atan2(v.Y, v.X));
    }

    public static float DeltaAngle(in Vector2 from, in Vector2 to) {
        return DeltaAngle(Atan2(from), to);
    }

    public static float DeltaAngle(in Vector3 from, in Vector3 to) {
        return DeltaAngle(Atan2(from), to);
    }

    public static float DeltaAngle(float fromDegree, Vector2 to) {
        return Normalize(Atan2(to) - fromDegree);
    }

    public static float DeltaAngle(float fromDegree, Vector3 to) {
        return Normalize(Atan2(to) - fromDegree);
    }

    private static float Normalize(float deg) {
        deg %= 360.0f;
        return deg switch {
            < -180.0f => deg + 360.0f,
            > 180.0f => deg - 360.0f,
            _ => deg
        };
    }
}
