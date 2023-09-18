using Godot;
using System;

namespace Taiju.Objects.Witch;

public partial class Spirit : Node3D {
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta) {
  Rotation = new Vector3(0.0f, Rotation.Y - (float)delta, 0.0f);
  }
}
