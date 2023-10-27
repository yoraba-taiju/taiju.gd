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
  private MultiMeshInstance3D multiMeshInstance_;
  
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
  private bool emitted_;
  private double emittedAt_;
  
  // Record
  private struct Record {
    public bool Emitted;
    public double InitialTime;
  }

  private Sparse<Record> record_;

  public override void _Ready() {
    base._Ready();
    Meshes = new MultiMesh();
    clockNode_ = GetNode<ClockNode>("/root/Root/Clock");
    multiMeshInstance_ = new MultiMeshInstance3D();
    multiMeshInstance_.Name = "SpritesNode";
    AddChild(multiMeshInstance_);
    Meshes.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
    Meshes.Mesh = Mesh;
    Meshes.UseColors = true;
    Meshes.InstanceCount = MeshCount;
    multiMeshInstance_.Multimesh = Meshes;
    items_ = new Item[MeshCount];
    bornAt_ = clockNode_.IntegrateTime;
    record_ = new Sparse<Record>(Clock, new Record {
      Emitted = false,
      InitialTime = 0.0,
    });
  }
  
  /*
   * Default overrides
   */

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref readonly var rec = ref record_.Ref;
    if (!record_.Ref.Emitted) {
      _Emit(ref items_);
      ref var recMut = ref record_.Mut;
      recMut.Emitted = true;
      recMut.InitialTime = integrateTime;
      _Update(ref items_, 0.0);
    } else {
      var time = integrateTime - rec.InitialTime;
      _Update(ref items_, time);
      if (time > Lifetime()) {
        Destroy();
      }
    }
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    integrateTime -= rec.InitialTime;
    _Update(ref items_, integrateTime);
    return false;
  }

  public override bool _ProcessLeap(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    integrateTime -= rec.InitialTime;
    _Update(ref items_, integrateTime);
    return true;
  }

  /*
   * Overrides
   */
  protected abstract void _Emit(ref Item[] items);

  protected abstract void _Update(ref Item[] items, double integrateTime);

  protected abstract double Lifetime();
}

