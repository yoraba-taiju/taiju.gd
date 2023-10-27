using Godot;
using Taiju.Objects.Witch;
using Taiju.Reversible.Gd;

namespace Taiju.Objects.Enemy; 

public abstract partial class EnemyBase : ReversibleRigidBody3D {
  protected Sora Sora { get; private set; }

  protected int Shield;
  public override void _Ready() {
    base._Ready();
    Sora = GetNode<Sora>("/root/Root/Field/Witch/Sora");
    Shield = 1;
  }

  public void Hit() {
    Shield -= 1;
    if (Shield <= 0) {
      Destroy();
    }
  }
}
