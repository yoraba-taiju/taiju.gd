using System;
using Godot;

namespace Taiju.Scenes;

public partial class Spawner : Node3D {
  private bool value_;
  [Export]
  public PackedScene value {
    get => value;
    set {
      if (value != null) {
        throw new Exception();
      }
    }
  }
}
