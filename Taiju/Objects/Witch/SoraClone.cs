using System.Collections.Generic;
using Godot;
using Taiju.Util.Reversible.Godot;
using Taiju.Util.Reversible.Value;

namespace Taiju.Objects.Witch;

public partial class SoraClone : ReversibleNode3D {
  [Export(PropertyHint.Range, "0,0.2,")] private double nextBulletDuration_ = 0.08;
  // Nodes
  private SoraBulletServer bulletServer_;
  private Node3D bulletNode_;

  // Replay data
  internal Sora.CloneType CloneType;
  internal DenseClone<Sora.Record> Replay;
  internal LinkedList<Sora.ShotRange> ShotRanges;
  internal double IntegrateTimeOffset;

  // Current statuses
  private struct Record {
    public double AfterFire;
    public LinkedListNode<Sora.ShotRange> CurrentRangeNode;
  }
  private Dense<Record> record_;

  public override void _Ready() {
    base._Ready();
    bulletServer_ = GetNode<SoraBulletServer>("/root/Root/Field/WitchBullet/SoraBulletServer")!;
    bulletNode_ = GetNode<Node3D>("/root/Root/Field/WitchBullet")!;
    record_ = new Dense<Record>(Clock, new Record { 
      AfterFire = double.NaN,
      CurrentRangeNode = ShotRanges.First,
    });
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    if (Replay.IsDead) {
      Destroy();
      return true;
    }
    LoadCurrentStatus();
    ref readonly var replay = ref Replay.Ref;
    ref var rec = ref record_.Mut;
    ref readonly var pos = ref replay.Position;

    // Handle shot
    InvokeFire(in replay, ref rec, integrateTime, dt);
    return true;
  }

  private void InvokeFire(ref readonly Sora.Record replay, ref Record rec, double integrateTime, double dt) {
    integrateTime = IntegrateTimeOffset + integrateTime;
    var currentRangeNode = rec.CurrentRangeNode;
    if (currentRangeNode == null) {
      return;
    }
    ref var afterFire = ref rec.AfterFire;
    ref readonly var pos = ref replay.Position;
    var currentRange = currentRangeNode.Value;

    // Next replay
    if (currentRange.EndTime < integrateTime) {
      currentRangeNode = rec.CurrentRangeNode.Next;
      rec.CurrentRangeNode = currentRangeNode;
      if (currentRangeNode == null) {
        return;
      }
      currentRange = currentRangeNode.Value;
    }

    if (currentRange.BeginTime <= integrateTime && integrateTime <= currentRange.EndTime) {
      if (double.IsNaN(afterFire)) {
        bulletServer_.Shot(pos + Vector3.Right * 2.0f);
        afterFire = nextBulletDuration_ * 1.3;
      } else {
        afterFire -= dt;
        if (afterFire < 0.0) {
          bulletServer_.ShotDouble(pos + Vector3.Right * 2.0f);
          afterFire += nextBulletDuration_;
        }
      }
    } else {
      afterFire = double.NaN;
    }
  }

  public override bool _ProcessBack(double integrateTime) {
    LoadCurrentStatus();
    return true;
  }

  private void LoadCurrentStatus() {
    if (!Replay.IsAlive) {
      return;
    }
    ref readonly var state = ref Replay.Ref;
    ref readonly var pos = ref state.Position;
    Position = pos;
  }
}
