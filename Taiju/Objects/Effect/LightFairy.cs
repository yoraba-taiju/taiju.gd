using Godot;
using Taiju.Objects.Reversible.Godot;
using Taiju.Util.Godot;

namespace Taiju.Objects.Effect;

public partial class LightFairy : ReversibleTubeTrail<LightFairy.Param> {
  [Export] public Color Color = Colors.PaleVioletRed;
  // Rotation
  [Export] public Vector3 CenterPosition = Vector3.Zero;
  [Export] public Vector3 Pole = Vector3.Up;
  private Vector3 poleRotatePole_;
  private Vector3 rod_ = Vector3.Right;
  [Export] public float Radius = 4.0f;
  [Export] public float RotateSpeed = 240.0f;
  [Export] public float PoleRotateSpeed = 80.0f;
  private RandomNumberGenerator rng_ = new();
  private MeshInstance3D sphereInstance_;
  private StandardMaterial3D sphereMaterial_;

  public struct Param;

  public override void _Ready() {
    TubeColors = new Color[Length];
    for (var i = 0; i < Length; ++i) {
      var c = Color;
      var a = (float)(Length - 1 - i) / Length;
      c.A = a * a * a;
      TubeColors[i] = c;
    }

    base._Ready();

    var r = Vec.RandomAngle(rng_);
    Pole = Pole.Normalized();
    rod_ = Pole.Cross(r).Normalized();
    poleRotatePole_ = Pole.Cross(rod_).Normalized();

    sphereInstance_ = GetNode<MeshInstance3D>("Sphere")!;
    sphereMaterial_ = (StandardMaterial3D)sphereInstance_.Mesh.SurfaceGetMaterial(0)!;
    sphereMaterial_.AlbedoColor = Color * 1.5f;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    base._ProcessForward(integrateTime, dt);
    var rotateSpeed = Mathf.DegToRad(RotateSpeed);
    var poleRotateSpeed = Mathf.DegToRad(PoleRotateSpeed);
    switch (integrateTime) {
      case <= 0.5: {
        var t = (float)integrateTime;
        var pos = CenterPosition + (rod_ * (Radius * t / 0.5f))
          .Rotated(Pole, rotateSpeed * (float)integrateTime)
          .Rotated(poleRotatePole_, poleRotateSpeed * (float)integrateTime);
        sphereInstance_.Position = pos;
        Push(pos, new Param());
      }
        break;

      case <= 3.5: {
        var t = (float)(integrateTime - 0.5);
        var pos = CenterPosition + (rod_ * Radius)
          .Rotated(Pole, rotateSpeed * (float)integrateTime)
          .Rotated(poleRotatePole_, poleRotateSpeed * (float)integrateTime);
        sphereInstance_.Position = pos;
        Push(pos, new Param());
      }
        break;

      case <= 4.0: {
        var t = (float)(integrateTime - 3.5);
        var pos = CenterPosition + (rod_ * (Radius * (0.5f - t) / 0.5f))
          .Rotated(Pole, rotateSpeed * (float)integrateTime)
          .Rotated(poleRotatePole_, poleRotateSpeed * (float)integrateTime);
        sphereInstance_.Position = pos;
        Push(pos, new Param());
      }
        break;

      default:
        Destroy();
        break;
    }
    return true;
  }
}
