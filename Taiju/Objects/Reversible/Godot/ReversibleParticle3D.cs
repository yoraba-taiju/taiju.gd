﻿using System;
using Godot;
using Taiju.Objects.Reversible.ValueArray;

namespace Taiju.Objects.Reversible.Godot;

// https://docs.godotengine.org/en/stable/tutorials/performance/vertex_animation/controlling_thousands_of_fish.html
public abstract partial class ReversibleParticle3D<T> : ReversibleNode3D
  where T: struct
{
  [Export] protected Mesh Mesh;
  [Export] protected int MeshCount = 16;
  [Export] protected float MaxSpeed = 10.0f;
  [Export(PropertyHint.Range, "1.0, 60.0")] protected double EmitPerSecond = 10;
  private readonly Transform2D transNaN_ = new Transform2D().TranslatedLocal(new Vector2(Single.NaN, Single.NaN));

  // https://docs.godotengine.org/en/stable/classes/class_multimesh.html
  private MultiMeshInstance3D multiMeshInstance_;
  
  // MeshData
  protected MultiMesh Meshes { get; private set; }

  struct ItemHolder {
    public bool Living;
    public double EmitAt;
    public T Item;
  }

  // Storages
  private ClockNode clockNode_;
  private SparseArray<ItemHolder> items_;
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
    items_ = new SparseArray<ItemHolder>(Clock, (uint)MeshCount, new ItemHolder());
    var span = items_.Mut;
    _EmitOne(ref span[0].Item);
    span[0].Living = true;
    span[0].EmitAt = 0.0;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ReadOnlySpan<ItemHolder> span = items_.Ref;
    Span<ItemHolder> spanMut = null;
    leftToEmit_ -= dt;
    if (leftToEmit_ <= 0) {
      leftToEmit_ += 1.0 / EmitPerSecond;
      for (var i = 0; i < MeshCount; ++i) {
        ref readonly var item = ref span[i];
        if (item.Living) {
          continue;
        }
        spanMut = spanMut != null ? spanMut : items_.Mut;
        ref var itemMut = ref spanMut[i];
        _EmitOne(ref itemMut.Item);
        itemMut.Living = true;
        itemMut.EmitAt = integrateTime;
        span = items_.Ref;
        break;
      }
    }

    for (var i = 0; i < MeshCount; ++i) {
      ref readonly var item = ref span[i];
      if (!item.Living || _Update(in item.Item, integrateTime - item.EmitAt)) {
        continue;
      }
      spanMut = spanMut != null ? spanMut : items_.Mut;
      ref var itemMut = ref spanMut[i];
      itemMut.Living = false;
      span = items_.Ref;
    }

    SetInstances(items_.Ref, integrateTime);
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

  private void SetInstances(ReadOnlySpan<ItemHolder> items, double integrateTime) {
    var globalPosition = GlobalPosition;
    for (var i = 0; i < MeshCount; ++i) {
      ref readonly var item = ref items[i];
      if (!item.Living) {
        Meshes.SetInstanceTransform2D(i, transNaN_);
        continue;
      }
      _SetInstance(i, in item.Item, integrateTime - item.EmitAt, globalPosition);
    }
  }

  protected abstract void _EmitOne(ref T item);

  protected abstract bool _Update(ref readonly T item, double t);
  protected abstract void _SetInstance(int i, ref readonly T item, double t, Vector3 currentGlobalPosition);
}

