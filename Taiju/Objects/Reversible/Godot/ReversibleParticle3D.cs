using System;
using Godot;
using Taiju.Objects.Reversible.ValueArray;

namespace Taiju.Objects.Reversible.Godot;

// https://docs.godotengine.org/en/stable/tutorials/performance/vertex_animation/controlling_thousands_of_fish.html
public abstract partial class ReversibleParticle3D<TParam> : ReversibleNode3D
  where TParam : struct
{
  [Export] protected Mesh Mesh;
  [Export] protected int MeshCount = 16;
  [Export] protected float MaxSpeed = 10.0f;
  [Export(PropertyHint.Range, "1.0, 60.0")] protected double EmitPerSecond = 10;

  private readonly Transform2D transZero_ = Transform2D.Identity.Scaled(Vector2.Zero);
  // https://docs.godotengine.org/en/stable/classes/class_multimesh.html
  private MultiMeshInstance3D multiMeshInstance_;
  
  // MeshData
  protected MultiMesh Meshes { get; private set; }

  struct Holder {
    public bool Living;
    public double EmitAt;
    public TParam Param;
  }

  // Storages
  private ClockNode clockNode_;
  private SparseArray<Holder> holders_;
  private double leftToEmit_;

  public override void _Ready() {
    base._Ready();
    Meshes = new MultiMesh();
    clockNode_ = GetNode<ClockNode>("/root/Root/Clock");
    multiMeshInstance_ = new MultiMeshInstance3D();
    AddChild(multiMeshInstance_);
    Meshes.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
    Meshes.Mesh = Mesh;
    Meshes.UseColors = true;
    Meshes.InstanceCount = MeshCount;
    multiMeshInstance_.Multimesh = Meshes;
    multiMeshInstance_.Name = "SpritesNode";
    holders_ = new SparseArray<Holder>(Clock, (uint)MeshCount, new Holder());
    var span = holders_.Mut;
    _EmitOne(ref span[0].Param);
    span[0].Living = true;
    span[0].EmitAt = 0.0;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ReadOnlySpan<Holder> span = holders_.Ref;
    Span<Holder> spanMut = null;
    leftToEmit_ -= dt;
    if (leftToEmit_ <= 0) {
      leftToEmit_ += 1.0 / EmitPerSecond;
      for (var i = 0; i < MeshCount; ++i) {
        ref readonly var item = ref span[i];
        if (item.Living) {
          continue;
        }
        spanMut = spanMut != null ? spanMut : holders_.Mut;
        ref var itemMut = ref spanMut[i];
        _EmitOne(ref itemMut.Param);
        itemMut.Living = true;
        itemMut.EmitAt = integrateTime;
        span = holders_.Ref;
        break;
      }
    }

    for (var i = 0; i < MeshCount; ++i) {
      ref readonly var item = ref span[i];
      if (!item.Living || _Update(in item.Param, integrateTime - item.EmitAt)) {
        continue;
      }
      spanMut = spanMut != null ? spanMut : holders_.Mut;
      ref var holderMut = ref spanMut[i];
      holderMut.Living = false;
      span = holders_.Ref;
    }

    SetInstances(holders_.Ref, integrateTime);
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    var span = holders_.Ref;
    SetInstances(span, integrateTime);
    return true;
  }

  public override bool _ProcessLeap(double integrateTime) {
    var span = holders_.Ref;
    SetInstances(span, integrateTime);
    return true;
  }

  private void SetInstances(ReadOnlySpan<Holder> holders, double integrateTime) {
    for (var i = 0; i < MeshCount; ++i) {
      ref readonly var holder = ref holders[i];
      if (!holder.Living) {
        Meshes.SetInstanceColor(i, Colors.Transparent);
        Meshes.SetInstanceTransform2D(i, transZero_);
        continue;
      }
      _SetInstance(i, in holder.Param, integrateTime - holder.EmitAt);
    }
  }

  protected abstract void _EmitOne(ref TParam item);

  protected abstract bool _Update(ref readonly TParam item, double t);
  protected abstract void _SetInstance(int i, ref readonly TParam item, double t);
}

