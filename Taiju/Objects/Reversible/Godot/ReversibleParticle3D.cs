using System;
using Godot;
using Taiju.Objects.Reversible.ValueArray;

namespace Taiju.Objects.Reversible.Godot;

// https://docs.godotengine.org/en/stable/tutorials/performance/vertex_animation/controlling_thousands_of_fish.html
public abstract partial class ReversibleParticle3D : ReversibleNode3D {
  [Export] protected Mesh Mesh;
  [Export] protected int MeshCount = 16;
  [Export] protected float MaxSpeed = 10.0f;
  [Export(PropertyHint.Range, "1.0, 60.0")] protected double EmitPerSecond;
  private static readonly Transform2D TransNaN = new Transform2D().TranslatedLocal(new Vector2(Single.NaN, Single.NaN));

  // https://docs.godotengine.org/en/stable/classes/class_multimesh.html
  private MultiMeshInstance3D multiMesh_;
  
  // MeshData
  protected MultiMesh Meshes { get; private set; }

  protected struct Item {
    public bool Living;
    public double EmitAt;
    public Color Color;
    public float Velocity;
    public Vector2 Angle;
    public double LifeTime;
  }

  // Storages
  private ClockNode clockNode_;
  private DenseArray<Item> items_;
  private double leftToEmit_;

  public override void _Ready() {
    base._Ready();
    Meshes = new MultiMesh();
    clockNode_ = GetNode<ClockNode>("/root/Root/Clock");
    multiMesh_ = GetNode<MultiMeshInstance3D>("MultiMesh");
    Meshes.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
    Meshes.Mesh = Mesh;
    Meshes.UseColors = true;
    Meshes.InstanceCount = MeshCount;
    multiMesh_.Multimesh = Meshes;
    items_ = new DenseArray<Item>(Clock, (uint)MeshCount, new Item());
    var span = items_.Mut;
    _EmitOne(ref span[0], 0.0);
  }
  
  public override bool _ProcessForward(double integrateTime, double dt) {
    var span = items_.Mut;
    leftToEmit_ -= dt;
    if (leftToEmit_ <= 0) {
      leftToEmit_ += 1.0 / EmitPerSecond;
      foreach (ref var item in span) {
        if (item.Living) {
          continue;
        }
        _EmitOne(ref item, integrateTime);
        item.Living = true;
        item.EmitAt = integrateTime;
        break;
      }
    }
    foreach (ref var item in span) {
      if (item.Living) {
        _Update(ref item, integrateTime);
      }
    }
    SetInstances(span, integrateTime);
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    var span = items_.Ref;
    SetInstances(span, integrateTime);
    return true;
  }

  public override bool _ProcessLeap(double integrateTime) {
    var span = items_.Ref;
    SetInstances(span, integrateTime);
    return true;
  }

  private void SetInstances(ReadOnlySpan<Item> items, double integrateTime) {
    for (var i = 0; i < MeshCount; ++i) {
      ref readonly var item = ref items[i];
      if (!item.Living) {
        Meshes.SetInstanceTransform2D(i, TransNaN);
        continue;
      }
      _SetInstance(i, in item, integrateTime);
    }
  }

  protected abstract void _EmitOne(ref Item item, double integrateTime);

  protected abstract void _Update(ref Item item, double integrateTime);
  protected abstract void _SetInstance(int i, ref readonly Item item, double integrateTime);
}

