using System;
using System.ComponentModel.DataAnnotations;
using Godot;
using Taiju.Util.Reversible.Value;
using Taiju.Util.Reversible.ValueArray;

namespace Taiju.Util.Reversible.Godot;

// https://docs.godotengine.org/en/stable/tutorials/performance/vertex_animation/controlling_thousands_of_fish.html
public abstract partial class ReversibleParticle3D<TParam> : ReversibleNode3D
  where TParam : struct
{
  private readonly Transform2D transZero_ = Transform2D.Identity.ScaledLocal(Vector2.Zero);

  [Export] public bool Emitting = true;
  [Export] protected Mesh Mesh;
  [Export] protected int MeshCount = 16;
  [Export] protected float MaxSpeed = 10.0f;
  [Export(PropertyHint.Range, "1.0, 60.0")] protected double EmitPerSecond = 10;
  protected IReversibleNode EmitterNode;
  [Export] private Node3D residueNode_;

  // Mesh Data
  private MultiMeshInstance3D multiMeshInstance_; // https://docs.godotengine.org/en/stable/classes/class_multimeshinstance3d.html
  protected MultiMesh Meshes { get; private set; } // https://docs.godotengine.org/en/stable/classes/class_multimesh.html
  private Node3D originalNode_;
  private Vector3 originalPosition_;

  // enum
  private enum State {
    Attached,
    Detached,
  }

  // Storages
  private struct Item {
    public bool Living;
    public double EmitAt;
    public double LifeTime;
    public TParam Param;
  }
  private SparseArray<Item> items_;
  private struct Record {
    public State State;
    public double TimeToEmit;
  }
  private Dense<Record> record_;

  public override void _Ready() {
    base._Ready();
    // Setup record
    record_ = new Dense<Record>(Clock, new Record {
      State = State.Attached,
      TimeToEmit = 0,
    });

    // Setup meshes
    Meshes = new MultiMesh();
    multiMeshInstance_ = new MultiMeshInstance3D();
    AddChild(multiMeshInstance_);

    Meshes.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
    Meshes.Mesh = Mesh;
    Meshes.UseColors = true;
    Meshes.InstanceCount = MeshCount;
    multiMeshInstance_.Multimesh = Meshes;
    multiMeshInstance_.Name = "SpritesNode";

    // Item management
    items_ = new SparseArray<Item>(Clock, (uint)MeshCount, new Item());

    // Set attach/detached process
    EmitterNode = FindEmitterNode();
    if (EmitterNode == null) {
      throw new ValidationException("No EmitterNode!");
    }
    originalNode_ = GetParent<Node3D>()!;
    originalPosition_ = Position;
    residueNode_ ??= GetNode<Node3D>("/root/Root/Field/EnemyEffect")!;
  }
  private IReversibleNode FindEmitterNode() {
    var parent = GetParent<Node>();
    while (parent != null) {
      if (parent is IReversibleNode node) {
        return node;
      }
      parent = parent.GetParent<Node>();
    }
    return null;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    var span = items_.Ref;
    Span<Item> spanMut = null;
    ref var record = ref record_.Mut;
    ref var timeToEmit = ref record.TimeToEmit;
    if (Emitting && record.State == State.Attached && Visible) {
      timeToEmit -= dt;
      if (timeToEmit < 0.0) {
        timeToEmit += 1.0 / EmitPerSecond;
        for (var i = 0; i < MeshCount; ++i) {
          ref readonly var item = ref span[i];
          if (item.Living) {
            continue;
          }
          spanMut = spanMut != null ? spanMut : items_.Mut;
          ref var itemMut = ref spanMut[i];
          var lifeTime = _Emit(i, ref itemMut.Param);
          itemMut.Living = true;
          itemMut.EmitAt = integrateTime;
          itemMut.LifeTime = lifeTime;
          span = items_.Ref;
          break;
        }
      }
    } else {
      timeToEmit = 0.0;
    }

    // Flag destroyed particle.
    var numLivingItem = 0;
    for (var i = 0; i < MeshCount; ++i) {
      ref readonly var item = ref span[i];
      if (item.Living == false) {
        continue;
      }
      numLivingItem++;
      var elapsed = integrateTime - item.EmitAt;
      var lifeTime = item.LifeTime;
      if (lifeTime < elapsed) {
        spanMut = spanMut != null ? spanMut : items_.Mut;
        ref var holderMut = ref spanMut[i];
        holderMut.Living = false;
        span = items_.Ref;
      }
    }
    UpdateItem(items_.Ref, integrateTime);

    if (record.State == State.Detached && numLivingItem == 0) {
      Destroy();
    }
    return true;
  }

  public override void _OnDestroy() {
    base._OnDestroy();
    CallDeferred("Detach");
  }

  private void Detach() {
    record_.Mut.State = State.Detached;
    Reparent(residueNode_);
  }

  public override bool _ProcessBack(double integrateTime) {
    var span = items_.Ref;
    ref readonly var record = ref record_.Ref;
    UpdateItem(span, integrateTime);
    UpdateParentOnReverse(record);
    return true;
  }

  public override bool _ProcessLeap(double integrateTime) {
    var span = items_.Ref;
    ref readonly var record = ref record_.Ref;
    UpdateItem(span, integrateTime);
    UpdateParentOnReverse(record);
    return true;
  }

  private void UpdateParentOnReverse(Record record) {
    switch (record.State) {
      case State.Attached: {
        var parent = GetParent<Node3D>()!;
        if (parent != originalNode_) {
          Reparent(originalNode_);
          Position = originalPosition_;
        }
      }
        break;
      case State.Detached:
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  private void UpdateItem(ReadOnlySpan<Item> holders, double integrateTime) {
    for (var i = 0; i < MeshCount; ++i) {
      ref readonly var holder = ref holders[i];
      if (holder.Living) {
        _UpdateItem(i, in holder.Param, holder.LifeTime, integrateTime - holder.EmitAt);
      } else {
        Meshes.SetInstanceTransform2D(i, transZero_);
      }
    }
  }

  protected abstract double _Emit(int i, ref TParam item);
  protected abstract void _UpdateItem(int i, ref readonly TParam item, double lifeTime, double t);
}
