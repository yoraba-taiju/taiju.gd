using System;
using Godot;
using Taiju.Util.Reversible.Value;

namespace Taiju.Util.Reversible.Godot;

// https://docs.godotengine.org/en/stable/tutorials/performance/vertex_animation/controlling_thousands_of_fish.html
public abstract partial class ReversibleOneShotParticle3D<TParam> : ReversibleNode3D
  where TParam: struct
{
  private readonly Transform2D transZero_ = Transform2D.Identity.ScaledLocal(Vector2.Zero);

  [Export] protected Mesh Mesh;
  [Export] private bool useMeshColor_ = true;
  [Export] protected int MeshCount = 16;

  // Mesh Data
  private MultiMeshInstance3D multiMeshInstance_; // https://docs.godotengine.org/en/stable/classes/class_multimeshinstance3d.html
  protected MultiMesh Meshes { get; private set; } // https://docs.godotengine.org/en/stable/classes/class_multimesh.html

  // Storages
  protected struct Item {
    public double LifeTime;
    public TParam Param;
  }
  private Item[] items_;
  private struct Record {
    public bool Emitted;
    public double EmittedAt;
    public double LifeTime;
  }
  private Sparse<Record> record_;

  public override void _Ready() {
    base._Ready();
    Meshes = new MultiMesh();
    multiMeshInstance_ = new MultiMeshInstance3D();
    multiMeshInstance_.Name = "SpritesNode";
    AddChild(multiMeshInstance_);

    Meshes.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
    Meshes.Mesh = Mesh;
    Meshes.UseColors = useMeshColor_;
    Meshes.InstanceCount = MeshCount;
    multiMeshInstance_.Multimesh = Meshes;

    // Item setup
    items_ = new Item[MeshCount];

    // Record
    record_ = new Sparse<Record>(Clock, new Record {
      Emitted = false,
      EmittedAt = 0.0,
      LifeTime = 0.0,
    });
  }
  
  /*
   * Default overrides
   */
  public override bool _ProcessForward(double integrateTime, double dt) {
    ref readonly var rec = ref record_.Ref;
    if (rec.Emitted) {
      var elapsed = integrateTime - rec.EmittedAt;
      var totalLifeTime = rec.LifeTime;
      if (elapsed <= totalLifeTime) {
        UpdateItem(elapsed);
      } else {
        Destroy();
      }
    } else {
      var maxLifeTime = 0.0;
      for (var i = 0; i < MeshCount; ++i) {
        ref var item = ref items_[i];
        var lifeTime = _Emit(i, ref item.Param);
        item.LifeTime = lifeTime;
        maxLifeTime = Math.Max(maxLifeTime, lifeTime);
      }
      ref var recMut = ref record_.Mut;
      recMut.Emitted = true;
      recMut.EmittedAt = integrateTime;
      recMut.LifeTime = maxLifeTime;
      UpdateItem(0.0);
    }
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    var elapsed = integrateTime - rec.EmittedAt;
    UpdateItem(elapsed);
    return false;
  }

  public override bool _ProcessLeap(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    var elapsed = integrateTime - rec.EmittedAt;
    UpdateItem(elapsed);
    return true;
  }

  private void UpdateItem(double elapsed) {
    for (var i = 0; i < MeshCount; ++i) {
      ref readonly var item = ref items_[i];
      var lifeTime = item.LifeTime;
      if (elapsed <= lifeTime) {
        _UpdateItem(i, in item.Param, lifeTime, elapsed);
      } else {
        Meshes.SetInstanceTransform2D(i, transZero_);
      }
    }
  }

  /*
   * Overrides
   */
  protected abstract double _Emit(int i, ref TParam param);
  protected abstract void _UpdateItem(int i, ref readonly TParam param, double lifeTime, double t);
}
