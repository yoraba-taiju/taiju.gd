using Godot;

namespace Taiju.Reversible.GD; 

public partial class ClockNode : Node3D {
  private Clock clock_;
  public ref Clock Clock => ref clock_;

  public override void _Ready() {
    Clock = new Clock();
  }
}
