using Godot;
using System;
using Taiju.Reversible.GD;
using Taiju.Reversible.Value;

namespace Taiju.Objects.Witch;

public partial class Spirit : ReversibleNode3D {
  private Dense<float> rot_;
  public override void _Ready() {
    base._Ready();
    rot_ = new Dense<float>(Clock, 0.0f);
  }

  public override void _Process(double dt) {
    ref var rot = ref rot_.Mut;
    rot += (float)dt;
    Rotation = new Vector3(0.0f, rot, 0.0f);
  }
}
