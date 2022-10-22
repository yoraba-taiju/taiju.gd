using System;
using Godot;

namespace Taiju.Plugins.Timeline; 

public partial class Spawner : AnimationPlayer {
  private Node3D field_;
  public override void _Ready() {
    field_ = GetNode<Node3D>("/root/Root/Field");
    Play("Scene");
  }
  private void Invoke(PackedScene packed) {
    var root = packed.Instantiate();
    foreach(var child in root.GetChildren()){
      root.RemoveChild(child);
      field_.AddChild(child);
    }
  }
}
