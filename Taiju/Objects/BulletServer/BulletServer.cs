using System;
using System.Collections.Generic;
using Godot;
using Taiju.Reversible.Gd;
using Taiju.Reversible.ValueArray;

namespace Taiju.Objects.BulletServer;

public abstract partial class BulletServer<TParam> : ReversibleNode3D
  where TParam: struct, IBullet
{
  [Export] private Mesh mesh_;
  private MultiMeshInstance3D multiMeshInstance3D_;
  private MultiMesh multiMesh_;
  private struct Bullet {
    public bool Living;
    public double SpawnAt;
    public TParam Param;
  }

  private Queue<TParam> spawnQueue_;

  private const int BulletCount = 64;

  private SparseArray<Bullet> bullets_;
  public override void _Ready() {
    base._Ready();
    multiMeshInstance3D_ = GetNode<MultiMeshInstance3D>("Mesh");
    multiMesh_ = new MultiMesh();
    multiMeshInstance3D_.Multimesh = multiMesh_;
    multiMesh_.Mesh = mesh_;
    multiMesh_.InstanceCount = BulletCount;
    multiMesh_.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
    bullets_ = new SparseArray<Bullet>(Clock, BulletCount, new Bullet {
      Living = false,
      SpawnAt = 0.0,
      Param = new TParam(),
    });
    spawnQueue_ = new Queue<TParam>();
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
    SpawnEnqueuedBullets(integrateTime);
    var bullets = bullets_.Ref;
    var meshes = multiMesh_;
    var zero = Transform2D.Identity.ScaledLocal(Vector2.Zero);
    for (var i = 0; i < BulletCount; ++i) {
      ref readonly var bullet = ref bullets[i];
      if (!bullet.Living) {
        meshes.SetInstanceTransform2D(i, zero);
        continue;
      }

      var pos = bullet.Param.PositionAt(integrateTime - bullet.SpawnAt);
      var trans = Transform2D.Identity.TranslatedLocal(pos);
      meshes.SetInstanceTransform2D(i, trans);
    }
  }

  private void SpawnEnqueuedBullets(double integrateTime) {
    if (spawnQueue_.Count == 0) {
      return;
    }
    var bullets = bullets_.Mut;
    while (spawnQueue_.TryDequeue(out var param)) {
      for (var i = 0; i < BulletCount; ++i) {
        if (bullets[i].Living) {
          continue;
        }

        bullets[i] = new Bullet {
          Living = true,
          SpawnAt = integrateTime,
          Param = param,
        };
      }
      Console.WriteLine("Failed to spawn bullet. Full.");
      break;
    }
  }

  public void Spawn(TParam item) {
    spawnQueue_.Enqueue(item);
  }
}
