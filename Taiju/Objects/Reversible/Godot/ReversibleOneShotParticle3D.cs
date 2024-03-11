using Godot;
using Taiju.Objects.Reversible.Value;

namespace Taiju.Objects.Reversible.Godot;

// https://docs.godotengine.org/en/stable/tutorials/performance/vertex_animation/controlling_thousands_of_fish.html
public abstract partial class ReversibleOneShotParticle3D : ReversibleNode3D {
  [Export] protected Mesh Mesh;
  [Export] protected int MeshCount = 16;
  [Export] protected float MaxSpeed = 10.0f;

  // https://docs.godotengine.org/en/stable/classes/class_multimesh.html
  private MultiMeshInstance3D multiMeshInstance_;
  
  // Lifetime
  private double lifeTime_;
  
  // MeshData
  private double bornAt_;
  protected MultiMesh Meshes { get; private set; }

  protected struct Item {
    public Color Color;
    public float Velocity;
    public Vector2 Direction;
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
    lifeTime_ = _Lifetime();
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
    if (!rec.Emitted) {
      _Emit(ref items_);
      ref var recMut = ref record_.Mut;
      recMut.Emitted = true;
      recMut.InitialTime = integrateTime;
      UpdateItem(integrateTime);
    } else {
      var time = integrateTime - rec.InitialTime;
      UpdateItem(integrateTime);
      if (time > lifeTime_) {
        Destroy();
      }
    }
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    integrateTime -= rec.InitialTime;
    UpdateItem(integrateTime);
    return false;
  }

  public override bool _ProcessLeap(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    integrateTime -= rec.InitialTime;
    UpdateItem(integrateTime);
    return true;
  }

  private void UpdateItem(double integrateTime) {
    for (var i = 0; i < MeshCount; ++i) {
      ref readonly var item = ref items_[i];
      UpdateItem(i, in item, integrateTime);
    }
  }

  /*
   * Overrides
   */
  protected abstract double _Lifetime();

  protected abstract void _Emit(ref Item[] items);

  protected abstract void UpdateItem(int i, ref readonly Item item, double integrateTime);
}
