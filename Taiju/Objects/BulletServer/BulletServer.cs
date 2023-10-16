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
  private MultiMeshInstance3D multiMeshInstance_;
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
    multiMeshInstance_ = new MultiMeshInstance3D();
    multiMeshInstance_.Name = "SpritesNode";
    AddChild(multiMeshInstance_);
    multiMesh_ = new MultiMesh();
    multiMeshInstance_.Multimesh = multiMesh_;
    multiMesh_.Mesh = mesh_;
    multiMesh_.UseCustomData = true;
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
    switch (ClockNode.Direction) {
      case ClockNode.TimeDirection.Forward:
        SpawnEnqueuedBullets(integrateTime);
        ProcessBullets(true, integrateTime);
        break;
      case ClockNode.TimeDirection.Stop:
      case ClockNode.TimeDirection.Back:
        ProcessBullets(false, integrateTime);
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }
  
  private void SpawnEnqueuedBullets(double integrateTime) {
    if (spawnQueue_.Count == 0) {
      return;
    }
    var bullets = bullets_.Mut;
    for (var i = 0; i < BulletCount; ++i) {
      if (bullets[i].Living) {
        continue;
      }

      var found = spawnQueue_.TryDequeue(out var param);
      if (!found) {
        break;
      }
      bullets[i] = new Bullet {
        Living = true,
        SpawnAt = integrateTime,
        Param = param,
      };
    }
    var leftBullets = spawnQueue_.Count;
    if (leftBullets <= 0) {
      return;
    }
    Console.WriteLine($"Failed to spawn {leftBullets} bullet. Full.");
    spawnQueue_.Clear();
  }
  
  private void ProcessBullets(bool forward, double integrateTime) {
    var bullets = bullets_.Ref;
    var meshes = multiMesh_;
    var ident = Transform2D.Identity;
    var zero = new Transform2D();
    for (var i = 0; i < BulletCount; ++i) {
      ref readonly var bullet = ref bullets[i];
      if (!bullet.Living) {
        meshes.SetInstanceTransform2D(i, zero);
        continue;
      }

      var attitude = bullet.Param.AttitudeAt(integrateTime - bullet.SpawnAt);
      var pos = attitude.Position;
      var angle = attitude.Angle;
      if (forward) {
        if (Mathf.Abs(pos.X) >= 25.0f || Mathf.Abs(pos.Y) >= 15.0f) {
          bullets_.Mut[i].Living = false;
          meshes.SetInstanceTransform2D(i, zero);
          continue;
        }
      }
      meshes.SetInstanceTransform2D(i, ident.TranslatedLocal(pos));
      meshes.SetInstanceCustomData(i, new Color {
        R = angle.X,
        G = angle.Y,
        B = 0.0f,
        A = 0.0f,
      });
    }
  }

  public void Spawn(TParam item) {
    spawnQueue_.Enqueue(item);
  }
}
