using Godot;

namespace Taiju.Reversible; 

public abstract partial class ReversibleNode3D : Node3D {
  public override void _Ready() {
    OnReady();
  }

  protected abstract void OnReady();

  public override void _PhysicsProcess(double delta) {
    OnPhysicsProcess(delta);
  }

  protected virtual void OnPhysicsProcess(double delta) {}

  public override void _Process(double delta) {
    OnProcess(delta);
  }

  protected abstract void OnProcess(double delta);
}
