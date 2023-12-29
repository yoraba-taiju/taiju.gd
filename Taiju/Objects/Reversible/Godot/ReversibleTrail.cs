using System;
using System.Collections.Generic;
using Godot;
using Taiju.Objects.Reversible.Value;
using Taiju.Objects.Reversible.ValueArray;

namespace Taiju.Objects.Reversible.Godot;

public abstract partial class ReversibleTrail<TParam> : ReversibleNode3D
  where TParam: struct {
  private const int BufferSize = 16;
  [Export(PropertyHint.Range, "1, 16")] protected int Length = 8;
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
    vertexes_.Clear();
    colors_.Clear();
    normals_.Clear();
    indexes_.Clear();
    var items = items_.Ref;
    ref readonly var idx = ref idx_.Ref;
    var points = 0;
    var vertexes = 0;
    for (var i = idx - Math.Min(Length, idx); i < idx; ++i) {
      ref readonly var item = ref items[i];
      var pos = item.Position;
      var dx = 1.0f;
      var dy = 1.0f;
      var dz = 1.0f;
      var color = Colors[points];
      {
        vertexes_.Add(pos + new Vector3(-dx, -dy, +dz));
        vertexes_.Add(pos + new Vector3(-dx, +dy, +dz));
        vertexes_.Add(pos + new Vector3(+dx, -dy, +dz));
        vertexes_.Add(pos + new Vector3(+dx, +dy, +dz));
        normals_.Add(new Vector3(0, 0, 1));
        normals_.Add(new Vector3(0, 0, 1));
        normals_.Add(new Vector3(0, 0, 1));
        normals_.Add(new Vector3(0, 0, 1));
        colors_.Add(color);
        colors_.Add(color);
        colors_.Add(color);
        colors_.Add(color);
        indexes_.Add(vertexes + 0);
        indexes_.Add(vertexes + 1);
        indexes_.Add(vertexes + 2);
        indexes_.Add(vertexes + 3);
        indexes_.Add(vertexes + 2);
        indexes_.Add(vertexes + 1);
        vertexes += 4;
      }
      {
        vertexes_.Add(pos + new Vector3(-dx, -dy, -dz));
        vertexes_.Add(pos + new Vector3(+dx, -dy, -dz));
        vertexes_.Add(pos + new Vector3(-dx, +dy, -dz));
        vertexes_.Add(pos + new Vector3(+dx, +dy, -dz));
        normals_.Add(new Vector3(0, 0, -1));
        normals_.Add(new Vector3(0, 0, -1));
        normals_.Add(new Vector3(0, 0, -1));
        normals_.Add(new Vector3(0, 0, -1));
        colors_.Add(color);
        colors_.Add(color);
        colors_.Add(color);
        colors_.Add(color);
        indexes_.Add(vertexes + 0);
        indexes_.Add(vertexes + 1);
        indexes_.Add(vertexes + 2);
        indexes_.Add(vertexes + 3);
        indexes_.Add(vertexes + 2);
        indexes_.Add(vertexes + 1);
        vertexes += 4;
      }
      ++points;
    }
    meshData_.Clear();
    meshData_.Resize((int)Mesh.ArrayType.Max);
    meshData_[(int)Mesh.ArrayType.Vertex] = vertexes_.ToArray();
    meshData_[(int)Mesh.ArrayType.Normal] = normals_.ToArray();
    meshData_[(int)Mesh.ArrayType.Color] = colors_.ToArray();
    meshData_[(int)Mesh.ArrayType.Index] = indexes_.ToArray();
    arrayMesh_.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, meshData_);
    var surfCount = arrayMesh_.GetSurfaceCount() - 1;
    arrayMesh_.SurfaceSetMaterial(surfCount, material_);
  }
}
