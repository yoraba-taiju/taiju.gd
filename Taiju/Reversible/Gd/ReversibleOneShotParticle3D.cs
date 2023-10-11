using System;
using Godot;
using Taiju.Reversible.Value;

namespace Taiju.Reversible.Gd;

// https://docs.godotengine.org/en/stable/tutorials/performance/vertex_animation/controlling_thousands_of_fish.html
public abstract partial class ReversibleOneShotParticle3D : ReversibleNode3D {
  [Export] protected Mesh Mesh;
  [Export] protected int MeshCount = 16;
  [Export] protected float MaxSpeed = 10.0f;

  // https://docs.godotengine.org/en/stable/classes/class_multimesh.html
  private MultiMeshInstance3D multiMesh_;
  
  // MeshData
  private double bornAt_;
  protected MultiMesh Meshes { get; private set; }

  protected struct Item {
    public Color Color;
    public float Velocity;
    public Vector2 Angle;
    public double LifeTime;
  }

  // Storages
  private ClockNode clockNode_;
  private Item[] items_;
  
  // Status
  private Sparse<Record> rec_;
  private struct Record {
    public bool Emitted;
    public double InitialTime;
  }

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
    items_ = new Item[MeshCount];
    bornAt_ = clockNode_.IntegrateTime;
    rec_ = new Sparse<Record>(Clock, new Record());
  }
  
  /*
   * Default overrides
   */

  public override bool _ProcessForward(double integrateTime, double dt) {
    return false;
  }

  public override bool _ProcessBack() {
    return false;
  }

  public override bool _ProcessLeap() {
    return false;
  }

  public override void _ProcessRaw(double integrateTime) {
    ref readonly var rec = ref rec_.Ref;
    if (!rec.Emitted) {
      _Emit(ref items_);
      ref var recMut = ref rec_.Mut;
      recMut.Emitted = true;
      recMut.InitialTime = integrateTime;
      Destroy();
    }
    _Update(ref items_, integrateTime - rec.InitialTime);
  }

  /*
   * Overrides
   */
  protected abstract void _Emit(ref Item[] items);

  protected abstract void _Update(ref Item[] items, double integrateTime);
}

