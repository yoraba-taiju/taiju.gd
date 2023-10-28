﻿using Godot;
using Taiju.Reversible.Gd;

namespace Taiju.Objects.Witch; 

public partial class SoraBulletServer : ReversibleNode3D {
  [Export] private PackedScene bulletScene_;
  public override bool _ProcessForward(double integrateTime, double dt) {
    return false;
  }

  public override bool _ProcessBack(double integrateTime) {
    return false;
  }

  public void Spawn(Vector3 pos) {
    var bullet = bulletScene_.Instantiate<SoraBullet>();
    bullet.Position = pos;
    AddChild(bullet);
  }
}