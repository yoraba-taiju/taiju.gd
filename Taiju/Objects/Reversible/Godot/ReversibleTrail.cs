using System;
using System.Collections.Generic;
using Godot;
using Taiju.Objects.Reversible.Value;
using Taiju.Objects.Reversible.ValueArray;

namespace Taiju.Objects.Reversible.Godot;

public abstract partial class ReversibleTrail<TParam> : ReversibleNode3D
  where TParam: struct
{
  private const int BufferSize = 16;
  [Export(PropertyHint.Range, "3, 16")] protected int Length = 8;
  [Export] private Curve tubeCurve_;
  [Export(PropertyHint.Range, "3, 24")] protected int TubeLength = 6;
  protected Color[] Colors = new Color[8];
  private global::Godot.Collections.Array meshData_ = new();
  private struct Item {
    public Vector3 Position;
    public TParam Param;
  }

  private MeshInstance3D meshInstance_ = new();
  private ArrayMesh arrayMesh_ = new();
  [Export] private Material material_;
  private DenseArray<Item> items_;
  private Dense<int> idx_;
  public override void _Ready() {
    base._Ready();
    items_ = new DenseArray<Item>(Clock, BufferSize, new Item());
    idx_ = new Dense<int>(Clock, 0);
    meshInstance_.Name = "MeshInstance3D";
    meshInstance_.Mesh = arrayMesh_;
    AddChild(meshInstance_);
  }

  protected void Push(Vector3 pos, TParam param) {
    ref var idx = ref idx_.Mut;
    var items = items_.Mut;
    items[idx] = new Item {
      Position = pos,
      Param = param,
    };
    ++idx;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    Render(integrateTime);
    // Debug purpose
    Rotation = new Vector3((float)integrateTime, 0, 0);
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    Render(integrateTime);
    return true;
  }
  public override bool _ProcessLeap(double integrateTime) {
    Render(integrateTime);
    return true;
  }

  private readonly List<Vector3> vertexes_ = [];
  private readonly List<Vector3> normals_ = [];
  private readonly List<Color> colors_ = [];
  private readonly List<int> indexes_ = [];
  private void Render(double integrateTime) {
    if (Length <= 3) {
      throw new InvalidOperationException("Invalid Length.");
    }
    var items = items_.Ref;
    ref readonly var currentIdx = ref idx_.Ref;
    if (currentIdx < 1) {
      throw new InvalidOperationException("No positions.");
    }
    vertexes_.Clear();
    colors_.Clear();
    normals_.Clear();
    indexes_.Clear();
    var points = 0;
    var vertexes = 0;
    var zero = currentIdx - Math.Min(Length, currentIdx);
    { // Triangle caps begin
      var begin = items[zero];
      var beginColor = Colors[0];
      var end = items[zero + 1];
      var endColor = Colors[1];
      var ring = tubeCurve_.Sample(1.0f / Length);
      var deltaX = end.Position - begin.Position;
      var deltaZ = new Vector3(0, 0, ring);
      var deltaY = deltaZ.Cross(deltaX).Normalized() * ring;
      var axis = deltaX.Normalized();
      var tan = Mathf.Tan(Mathf.Pi / TubeLength); // ((Pi * 2) / (TubeLength * 2))
      for (var tubeIdx = 0; tubeIdx < TubeLength; ++tubeIdx) {
        var dz = deltaZ.Rotated(axis, Mathf.Pi * 2 * tubeIdx / TubeLength);
        var dy = deltaY.Rotated(axis, Mathf.Pi * 2 * tubeIdx / TubeLength).Normalized() * (ring * tan);
        vertexes_.Add(begin.Position);
        vertexes_.Add(end.Position + dz + dy);
        vertexes_.Add(end.Position + dz - dy);
        normals_.Add(dz);
        normals_.Add(dz);
        normals_.Add(dz);
        colors_.Add(beginColor);
        colors_.Add(endColor);
        colors_.Add(endColor);
        indexes_.Add(vertexes + 2);
        indexes_.Add(vertexes + 1);
        indexes_.Add(vertexes + 0);
        vertexes += 3;
      }
    }
    for (var i = zero + 1; i < currentIdx - 1; ++i) {
      ++points;
    }
    meshData_.Clear();
    meshData_.Resize((int)Mesh.ArrayType.Max);
    meshData_[(int)Mesh.ArrayType.Vertex] = vertexes_.ToArray();
    meshData_[(int)Mesh.ArrayType.Normal] = normals_.ToArray();
    meshData_[(int)Mesh.ArrayType.Color] = colors_.ToArray();
    meshData_[(int)Mesh.ArrayType.Index] = indexes_.ToArray();
    arrayMesh_.ClearSurfaces();
    arrayMesh_.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, meshData_);
    var surfCount = arrayMesh_.GetSurfaceCount() - 1;
    arrayMesh_.SurfaceSetMaterial(surfCount, material_);
  }
}
