using System;
using Godot;
using Taiju.Reversible.ValueArray;

namespace Taiju.Reversible.Gd;

// https://docs.godotengine.org/en/stable/tutorials/performance/vertex_animation/controlling_thousands_of_fish.html
public abstract partial class ReversibleParticle3D : ReversibleNode3D {
  [Export] protected Mesh Mesh;
  [Export] protected int MeshCount = 16;
  [Export] protected float MaxSpeed = 10.0f;
  [Export(PropertyHint.Range, "1.0, 60.0")] protected double EmitPerSecond;

  // https://docs.godotengine.org/en/stable/classes/class_multimesh.html
  private MultiMeshInstance3D multiMesh_;
  
  // MeshData
  private double bornAt_;
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
  private SparseArray<Item> items_;
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
    items_ = new SparseArray<Item>(Clock, (uint)MeshCount, new Item());
    bornAt_ = clockNode_.IntegrateTime;
    var span = items_.Mut;
    _Emit(ref span, 0.0);
  }
  
  public override bool _ProcessForward(double integrateTime, double dt) {
    leftToEmit_ -= dt;
    if (leftToEmit_ <= 0) {
      leftToEmit_ += EmitPerSecond;
      var spanMut = items_.Mut;
      _Emit(ref spanMut, integrateTime);
    }

    var span = items_.Ref;
    _Update(ref span, integrateTime);
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    return true;
  }

  public override bool _ProcessLeap(double integrateTime) {
    // Do nothing.
    return true;
  }

  protected abstract void _Emit(ref Span<Item> items, double integrateTime);

  protected abstract void _Update(ref ReadOnlySpan<Item> items, double integrateTime);
}

