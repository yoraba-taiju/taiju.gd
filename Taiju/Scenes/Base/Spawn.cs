using Godot;
using Taiju.Objects.Enemy;

namespace Taiju.Scenes.Base;

[Tool]
public partial class Spawn : Area2D {
  [Export] private PackedScene enemy_;

  public EnemyBase Instantiate() {
    return enemy_.Instantiate<EnemyBase>();
  }
}
