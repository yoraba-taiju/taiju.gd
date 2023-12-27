using System;
using System.Collections.Generic;
using Godot;
using Taiju.Objects.Witch;
using Taiju.Objects.Reversible.Godot;
using Taiju.Objects.Reversible.ValueArray;

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
    for (var i = 1; i <= 20; ++i) {
      multiMeshInstance_.SetLayerMaskValue(i, i == 20);
    }
    AddChild(multiMeshInstance_);
    // Initialize
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
    }
    var leftBullets = spawnQueue_.Count;
    if (leftBullets <= 0) {
      return;
    }
    Console.WriteLine($"Failed to spawn {leftBullets} bullet. Full (of {bulletCount_} bullets).");
    spawnQueue_.Clear();
  }
  
  private readonly Transform2D transZero_ = Transform2D.Identity.Scaled(Vector2.Zero);
  private void ProcessBullets(bool forward, double integrateTime) {
    var bullets = Bullets.Ref;
    var meshes = multiMesh_;
    for (var i = 0; i < bulletCount_; ++i) {
      ref readonly var bullet = ref bullets[i];
      if (!bullet.Living) {
        meshes.SetInstanceTransform2D(i, transZero_);
        continue;
      }

      var attitude = bullet.Param.AttitudeAt(integrateTime - bullet.SpawnAt);
      var pos = attitude.Position;
      var angle = attitude.Angle;
      var resp = OnBulletMove(attitude);

      if (resp == Response.HitToSora) {
        Sora.Hit();
      }

      // Live/Dead
      var forwardCond = Mathf.Abs(pos.X) >= 25.0f || Mathf.Abs(pos.Y) >= 15.0f || resp != Response.None;
      var backCond = integrateTime <= bullet.SpawnAt;
      if ((forward && forwardCond) || (!forward && backCond)) {
        Bullets.Mut[i].Living = false; // State changed!
        meshes.SetInstanceTransform2D(i, transZero_);
        continue;
      }
      meshes.SetInstanceTransform2D(i, Transform2D.Identity.TranslatedLocal(pos).RotatedLocal(angle.Angle()));
    }
  }

  protected void Spawn(TParam item) {
    spawnQueue_.Enqueue(item);
  }

  protected abstract Response OnBulletMove(IBullet.Attitude attitude);
}
