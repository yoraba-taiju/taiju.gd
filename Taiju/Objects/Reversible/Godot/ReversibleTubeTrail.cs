﻿using System;
using System.Collections.Generic;
using Godot;
using Taiju.Objects.Reversible.Value;
using Taiju.Objects.Reversible.ValueArray;

namespace Taiju.Objects.Reversible.Godot;

public abstract partial class ReversibleTubeTrail<TParam> : ReversibleNode3D
  where TParam: struct
{
  private const int BufferSize = 16;
  [Export(PropertyHint.Range, "3, 16")] protected int Length = 8;
  [Export] private Curve tubeCurve_;
  [Export(PropertyHint.Range, "3, 24")] protected int TubeLength = 6;
  protected Color[] TubeColors;
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
    // Pre-conditions assertion
    if (Length <= 3) {
      throw new InvalidOperationException($"Invalid Length. {Length} < 3");
    }
    if (Length != TubeColors.Length) {
      throw new InvalidOperationException($"Length should be ColorsLength. (Length = {Length}) != (Colors.Length ={TubeColors.Length})");
    }
    if (TubeLength < 3) {
      throw new InvalidOperationException($"Invalid TubeLength. {TubeLength} < 3");
    }
    items_ = new DenseArray<Item>(Clock, BufferSize, new Item {
      Position = Position,
      Param = new TParam(),
    });
    idx_ = new Dense<int>(Clock, 0);
    meshInstance_.Name = "MeshInstance3D";
    meshInstance_.Mesh = arrayMesh_;
    AddChild(meshInstance_);
  }

  protected void Push(Vector3 pos, TParam param) {
    ref var idx = ref idx_.Mut;
    var items = items_.Mut;
    var last = items[idx % BufferSize].Position;
    var distance = (last - pos).Length();
    if (distance is < 0.001f or > 1000.0f) {
      return;
    }
    items[idx % BufferSize] = new Item {
      Position = pos,
      Param = param,
    };
    ++idx;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    Render();
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    Render();
    return true;
  }
  public override bool _ProcessLeap(double integrateTime) {
    Render();
    return true;
  }

  private readonly List<Vector3> vertexes_ = [];
  private readonly List<Vector3> normals_ = [];
  private readonly List<Color> colors_ = [];
  private readonly List<int> indexes_ = [];
  private void Render() {
    var items = items_.Ref;
    ref readonly var currentIdx = ref idx_.Ref;
    if (currentIdx < 3) {
      // GD.Print($"Not enough length: {currentIdx} < 3");
      return;
    }
    vertexes_.Clear();
    colors_.Clear();
    normals_.Clear();
    indexes_.Clear();
    var points = 0;
    var vertexes = 0;
    var tan = Mathf.Tan(Mathf.Pi / TubeLength); // ((Pi * 2) / (TubeLength * 2))
    var zero = currentIdx - Math.Min(Length, currentIdx);
    Vector3 deltaY0;
    Vector3 deltaZ0;
    Vector3 axis0;
    float ring0;
    var length = currentIdx - zero;
    { // Triangle caps begin
      var begin = items[(currentIdx - 1) % BufferSize];
      var beginColor = TubeColors[0];
      var end = items[(currentIdx - 2) % BufferSize];
      var endColor = TubeColors[1];
      var ring = tubeCurve_.Sample(1.0f / length);
      var deltaX = end.Position - begin.Position;
      var deltaZ = Mathf.Abs(deltaX.X) < 0.1
        ? new Vector3(0.0f, 0.0f, 1.0f).Normalized() * ring
        : new Vector3(deltaX.Z / deltaX.X, 0, 1).Normalized() * ring;
      var deltaY = deltaZ.Cross(deltaX).Normalized();
      var axis = deltaX.Normalized();
      for (var tubeIdx = 0; tubeIdx < TubeLength; ++tubeIdx) {
        var dz = deltaZ.Rotated(axis, Mathf.Pi * 2 * tubeIdx / TubeLength);
        var dy = deltaY.Rotated(axis, Mathf.Pi * 2 * tubeIdx / TubeLength) * (ring * tan);
        vertexes_.Add(begin.Position);
        vertexes_.Add(end.Position + dz + dy);
        vertexes_.Add(end.Position + dz - dy);
        normals_.Add(-axis);
        normals_.Add((dz + dy).Normalized());
        normals_.Add((dz - dy).Normalized());
        colors_.Add(beginColor);
        colors_.Add(endColor);
        colors_.Add(endColor);
        indexes_.Add(vertexes + 0);
        indexes_.Add(vertexes + 1);
        indexes_.Add(vertexes + 2);
        vertexes += 3;
      }
      deltaY0 = deltaY;
      deltaZ0 = deltaZ;
      axis0 = axis;
      ring0 = ring;
      ++points;
    }
    for (var i = currentIdx - 2; i > zero + 1; --i) {
      var beginPoint = items[i % BufferSize].Position;
      var endPoint = items[(i - 1) % BufferSize].Position;
      var beginColor = TubeColors[points];
      var endColor = TubeColors[points + 1];
      var deltaX = endPoint - beginPoint;
      var axis = deltaX.Normalized();
      var ring = tubeCurve_.Sample((float)(points + 1) / length);
      var deltaZ = Mathf.Abs(deltaX.X) < 0.1
        ? new Vector3(0.0f, 0.0f, 1.0f).Normalized() * ring
        : new Vector3(deltaX.Z / deltaX.X, 0, 1).Normalized() * ring;
      var deltaY = deltaZ.Cross(deltaX).Normalized();
      for (var tubeIdx = 0; tubeIdx < TubeLength; ++tubeIdx) {
        var dz0 = deltaZ0.Rotated(axis0, Mathf.Pi * 2 * tubeIdx / TubeLength);
        var dy0 = deltaY0.Rotated(axis0, Mathf.Pi * 2 * tubeIdx / TubeLength) * (ring0 * tan);
        var dz = deltaZ.Rotated(axis, Mathf.Pi * 2 * tubeIdx / TubeLength);
        var dy = deltaY.Rotated(axis, Mathf.Pi * 2 * tubeIdx / TubeLength) * (ring * tan);
        vertexes_.Add(beginPoint + dz0 + dy0);
        vertexes_.Add(beginPoint + dz0 - dy0);
        vertexes_.Add(endPoint + dz + dy);
        vertexes_.Add(endPoint + dz - dy);
        normals_.Add((dz0 + dy0).Normalized());
        normals_.Add((dz0 - dy0).Normalized());
        normals_.Add((dz + dy).Normalized());
        normals_.Add((dz - dy).Normalized());
        colors_.Add(beginColor);
        colors_.Add(beginColor);
        colors_.Add(endColor);
        colors_.Add(endColor);
        indexes_.Add(vertexes + 2);
        indexes_.Add(vertexes + 1);
        indexes_.Add(vertexes + 0);
        indexes_.Add(vertexes + 1);
        indexes_.Add(vertexes + 2);
        indexes_.Add(vertexes + 3);
        vertexes += 4;
      }
      deltaY0 = deltaY;
      deltaZ0 = deltaZ;
      axis0 = axis;
      ring0 = ring;
      ++points;
    }
    { // Triangle caps end
      var begin = items[(zero + 1) % BufferSize];
      var beginColor = TubeColors[points];
      var end = items[zero % BufferSize];
      var endColor = TubeColors[points + 1];
      var deltaX = end.Position - begin.Position;
      var deltaZ = Mathf.Abs(deltaX.X) < 0.1
        ? new Vector3(0.0f, 0.0f, 1.0f).Normalized() * ring0
        : new Vector3(deltaX.Z / deltaX.X, 0, 1).Normalized() * ring0;
      var deltaY = deltaZ.Cross(deltaX).Normalized();
      var axis = deltaX.Normalized();
      for (var tubeIdx = 0; tubeIdx < TubeLength; ++tubeIdx) {
        var dz = deltaZ.Rotated(axis, Mathf.Pi * 2 * tubeIdx / TubeLength);
        var dy = deltaY.Rotated(axis, Mathf.Pi * 2 * tubeIdx / TubeLength) * (ring0 * tan);
        vertexes_.Add(begin.Position + dz + dy);
        vertexes_.Add(begin.Position + dz - dy);
        vertexes_.Add(end.Position);
        normals_.Add((dz + dy).Normalized());
        normals_.Add((dz - dy).Normalized());
        normals_.Add(axis);
        colors_.Add(beginColor);
        colors_.Add(endColor);
        colors_.Add(endColor);
        indexes_.Add(vertexes + 2);
        indexes_.Add(vertexes + 1);
        indexes_.Add(vertexes + 0);
        vertexes += 3;
      }
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
