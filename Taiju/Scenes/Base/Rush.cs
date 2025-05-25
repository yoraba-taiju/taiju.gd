using Godot;
using Taiju.Objects.Rush;

namespace Taiju.Scenes.Base;

public partial class Rush : Area2D {
  [Export] private PackedScene rush_;

  public RushBase Instantiate() {
    return rush_.Instantiate<RushBase>();
  }
}
