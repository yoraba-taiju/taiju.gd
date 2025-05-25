using System;
using Godot;
using Taiju.Util.Reversible.Godot;
using Taiju.Util.Reversible.Value;
using Taiju.Util.Reversible.ValueArray;

namespace Taiju.Objects.Effect;

public partial class Spirit : ReversibleNode3D {
  // TODO: LODを無効にしないとglitchが出る
  // https://github.com/godotengine/godot/issues/104160
  private Skeleton3D bones_;
  private const int NumBoneIndices = 10;
  private const int Interval = 2;
  private const int NumModelPosition = NumBoneIndices * Interval;
  [Export] private Color color_ = Colors.Crimson;
  [Export] private double radius_ = 3.0;
  [Export] private Vector3 upVector_ = Vector3.Forward;
  private Vector3 radVector_ = Vector3.Forward;
  [Export] private double speedInDegree_ = 360.0 / 3.0;
  private Dense<Record> rec_;
  private DenseArray<Vector3> modelPositions_;
  private Node3D model_;
  private static readonly Vector3[] CrossVectors = [
    new Vector3(1.0f, 0.0f, 0.0f).Normalized(),
    new Vector3(0.0f, 1.0f, 0.0f).Normalized(),
    new Vector3(0.0f, 0.0f, 1.0f).Normalized(),

    new Vector3(1.0f, 1.0f, 0.0f).Normalized(),
    new Vector3(0.0f, 1.0f, 1.0f).Normalized(),
    new Vector3(1.0f, 0.0f, 1.0f).Normalized(),

    new Vector3(1.0f, 1.0f, 1.0f).Normalized(),
  ];
  private struct Record {
    public int ModelPositionIndex;
  }

  public override void _Ready() {
    base._Ready();
    model_ = GetNode<Node3D>("Model");
    bones_ = model_.GetNode<Skeleton3D>("Armature/Skeleton3D");
    {
      var core = model_.GetNode<MeshInstance3D>("Armature/Skeleton3D/Core");
      var material = (StandardMaterial3D)core.GetSurfaceOverrideMaterial(0);
      var original = material.Emission;
      var color = Color.FromHsv(color_.H, original.S, original.V);
      material.Emission = color;
    }
    rec_ = new Dense<Record>(Clock, new Record {
      ModelPositionIndex = 0,
    });
    upVector_ = upVector_.Normalized();
    radVector_ = Vector3.Zero;
    foreach (var v in CrossVectors) {
      if (Mathf.Abs(upVector_.Dot(v)) >= 0.8f) {
        continue;
      }
      radVector_ = upVector_.Cross(v).Normalized() * (float)radius_;
      break;
    }
    if (radVector_.IsZeroApprox()) {
      throw new ArgumentException("UpVector is invalid");
    }
    modelPositions_ = new DenseArray<Vector3>(Clock, NumModelPosition, Position);

    // Initial
    UpdateModelPosition(ref rec_.Mut, modelPositions_.Mut, 0.0);
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var rec = ref rec_.Mut;
    var modelPositions = modelPositions_.Mut;
    UpdateModelPosition(ref rec, modelPositions, integrateTime);
    SetModelPosition(rec, modelPositions);
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    ref readonly var rec = ref rec_.Ref;
    var modelPositions = modelPositions_.Ref;

    SetModelPosition(rec, modelPositions);
    return true;
  }

  private void UpdateModelPosition(ref Record rec, Span<Vector3> modelPositions, double integrateTime) {
    var rad = Mathf.DegToRad((360.0 / 3) * integrateTime);
    ref var modelPositionIndex = ref rec.ModelPositionIndex;
    var pos = Position + radVector_.Rotated(upVector_, (float)rad);
    modelPositions[modelPositionIndex % NumModelPosition] = pos;
    modelPositionIndex++;
  }

  private void SetModelPosition(in Record rec, ReadOnlySpan<Vector3> modelPositions) {
    ref readonly var modelPositionIndex = ref rec.ModelPositionIndex;
    if (modelPositionIndex == 0) {
      return;
    }
    // Position
    model_.Position = modelPositions[(modelPositionIndex - 1) % NumModelPosition];

    // Bone
    bones_.ResetBonePoses();
    switch (modelPositionIndex) {
      case <= Interval * 3:
        for (var i = 0; i < NumBoneIndices; ++i) {
          switch (i) {
            case 0:
              bones_.SetBonePose(0,
                Transform3D.Identity
                  .TranslatedLocal(new Vector3(0.0f, 0.5f, 0.0f))
                  .Rotated(Vector3.Left, Mathf.DegToRad(90.0f)));
              break;
            case <= 2:
              bones_.SetBonePosePosition(i, new Vector3(0.0f, 0.5f, 0.0f));
              break;
            case 3:
              bones_.SetBonePosePosition(i, new Vector3(0.0f, 0.1f, 0.0f));
              bones_.SetBonePoseScale(i, Vector3.One * 0.7f);
              break;
            default:
              bones_.SetBonePose(i, Transform3D.Identity.TranslatedLocal(new Vector3(0.0f, 0.5f, 0.0f)));
              bones_.SetBonePoseScale(i, Vector3.Zero);
              break;
          }
        }
        break;
      default: {
        var lastDir = Vector3.Up;
        var lastModelPosition = modelPositionIndex - 1;
        for (var i = 0; i < NumBoneIndices; ++i) {
          var idx0 = lastModelPosition - i * Interval;
          var idx1 = lastModelPosition - (i + 1) * Interval;
          if (idx1 < 0) {
            switch (i) {
              case <= 2:
                bones_.SetBonePosePosition(i, new Vector3(0.0f, 0.5f, 0.0f));
                break;
              case 3:
                bones_.SetBonePosePosition(i, new Vector3(0.0f, 0.1f, 0.0f));
                bones_.SetBonePoseScale(i, Vector3.One * 0.7f);
                break;
              default:
                bones_.SetBonePosePosition(i, new Vector3(0.0f, 0.5f, 0.0f));
                bones_.SetBonePoseScale(i, Vector3.Zero);
                break;
            }
            continue;
          }
          var pos0 = modelPositions[idx0 % NumModelPosition];
          var pos1 = modelPositions[idx1 % NumModelPosition];
          var dir = pos1 - pos0;
          var normalizedDir = dir.Normalized();
          var q = new Quaternion(lastDir, normalizedDir).Normalized();
          bones_.SetBonePosePosition(i, new Vector3(0.0f, dir.Length(), 0.0f));
          bones_.SetBonePoseRotation(i, q);
          lastDir = normalizedDir;
        }
      }
        break;
    }
  }
}
