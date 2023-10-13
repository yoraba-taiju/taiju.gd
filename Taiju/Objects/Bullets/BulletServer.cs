using Godot;
using Taiju.Reversible.Gd;
using Taiju.Reversible.ValueArray;

namespace Taiju.Objects.Bullets;

public abstract partial class BulletServer : ReversibleNode3D {
  [Export] private Mesh mesh_;
  private MultiMeshInstance3D multiMeshInstance3D_;
  private MultiMesh multiMesh_;
  private struct BulletState {
    public bool Living;
    public Vector3 Position;
    public double SpawnAt;
  }

  private const int BulletCount = 256;

  private SparseArray<BulletState> bullets_;
  public override void _Ready() {
    base._Ready();
    multiMeshInstance3D_ = GetNode<MultiMeshInstance3D>("Mesh");
    multiMesh_ = new MultiMesh();
    multiMeshInstance3D_.Multimesh = multiMesh_;
    multiMesh_.Mesh = mesh_;
    multiMesh_.UseColors = true;
    multiMesh_.InstanceCount = BulletCount;
    bullets_ = new SparseArray<BulletState>(Clock, BulletCount, new BulletState {
      Living = false,
      Position = Vector3.Zero,
      SpawnAt = 0.0,
    });
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    return false;
  }

  public override bool _ProcessBack(double integrateTime) {
    return false;
  }

  public override bool _ProcessLeap(double integrateTime) {
    return false;
  }

  public override void _ProcessRaw(double integrateTime) {
    base._ProcessRaw(integrateTime);
  }
}
