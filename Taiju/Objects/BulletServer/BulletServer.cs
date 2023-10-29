using System;
using System.Collections.Generic;
using Godot;
using Taiju.Objects.Witch;
using Taiju.Reversible.Gd;
using Taiju.Reversible.ValueArray;

namespace Taiju.Objects.BulletServer;

public abstract partial class BulletServer<TParam> : ReversibleNode3D
  where TParam: struct, IBullet
{
  [Export] protected Mesh Mesh;
  private MultiMeshInstance3D multiMeshInstance_;
  private MultiMesh multiMesh_;
  protected struct Bullet {
    public bool Living;
    public double SpawnAt;
    public TParam Param;
  }

  protected enum Response {
    None,
    HitToSora,
  }

  private Queue<TParam> spawnQueue_;

  [Export] private uint bulletCount_ = 64;

  protected SparseArray<Bullet> Bullets;

  protected Sora Sora;
  public override void _Ready() {
    base._Ready();
    multiMeshInstance_ = new MultiMeshInstance3D();
    multiMeshInstance_.Name = "SpritesNode";
    multiMesh_ = new MultiMesh();
    multiMesh_.Mesh = Mesh;
    multiMesh_.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
    multiMesh_.UseColors = false;
    multiMesh_.UseCustomData = false;
    multiMesh_.InstanceCount = (int)bulletCount_;
    multiMeshInstance_.Multimesh = multiMesh_;
    AddChild(multiMeshInstance_);
    Bullets = new SparseArray<Bullet>(Clock, bulletCount_, new Bullet {
      Living = false,
      SpawnAt = 0.0,
      Param = new TParam(),
    });
    spawnQueue_ = new Queue<TParam>();
    Sora = GetNode<Sora>("/root/Root/Field/Witch/Sora");
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
    var bullets = Bullets.Mut;
    var meshes = multiMesh_;
    for (var i = 0; i < bulletCount_; ++i) {
      ref var bullet = ref bullets[i];
      if (bullet.Living) {
        continue;
      }

      var found = spawnQueue_.TryDequeue(out var param);
      if (!found) {
        break;
      }

      bullet = new Bullet {
        Living = true,
        SpawnAt = integrateTime,
        Param = param,
      };
      meshes.SetInstanceColor(i, Colors.White);
    }
    var leftBullets = spawnQueue_.Count;
    if (leftBullets <= 0) {
      return;
    }
    Console.WriteLine($"Failed to spawn {leftBullets} bullet. Full.");
    spawnQueue_.Clear();
  }
  
  private void ProcessBullets(bool forward, double integrateTime) {
    var bullets = Bullets.Mut;
    var meshes = multiMesh_;
    var ident = Transform2D.Identity;
    for (var i = 0; i < bulletCount_; ++i) {
      ref var bullet = ref bullets[i];
      if (!bullet.Living) {
        continue;
      }

      var attitude = bullet.Param.AttitudeAt(integrateTime - bullet.SpawnAt);
      var pos = attitude.Position;
      var angle = attitude.Angle;
      var resp = OnBulletMove(attitude);
      if (forward && (Mathf.Abs(pos.X) >= 25.0f || Mathf.Abs(pos.Y) >= 15.0f || resp != Response.None)) {
        bullet.Living = false;
        meshes.SetInstanceTransform2D(i, ident.TranslatedLocal(Vector2.One * float.NaN));
        continue;
      }
      switch (resp) {
        case Response.None:
          break;
        case Response.HitToSora:
          Sora.Hit();
          break;
      }
      meshes.SetInstanceTransform2D(i, ident.TranslatedLocal(pos).RotatedLocal(angle.Angle()));
    }
  }

  protected void Spawn(TParam item) {
    spawnQueue_.Enqueue(item);
  }

  protected abstract Response OnBulletMove(IBullet.Attitude attitude);
}
