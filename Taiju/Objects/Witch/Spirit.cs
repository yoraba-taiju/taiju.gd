using Godot;
using System;

namespace Taiju.Objects.Witch;

public partial class Spirit : Node3D {
  public override void _Ready() {
  }

  public override void _Process(double delta) {
    Rotation = new Vector3(0.0f, Rotation.Y - (float)delta, 0.0f);
  }
}
