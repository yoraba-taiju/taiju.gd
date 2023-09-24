using System;
using Godot;

namespace Taiju.Scenes;

public partial class Spawner : Node3D {
  [Export] public PackedScene Value;
}
