using Godot;

namespace Taiju.Reversible.GD; 

public abstract partial class ReversibleNode3D : Node3D, IReversibleNode {
  /// Members
  private ClockNode clockNode_;
  private Clock clock_;

  /// Accessors
  protected ClockNode ClockNode => clockNode_;
  protected Clock Clock => clock_;
  /// Clock Stats
  public double IntegrateTime => clockNode_.IntegrateTime;
  protected bool Forward => ClockNode.Forward;
  protected bool Back => ClockNode.Forward;
  protected bool Ticked => ClockNode.Ticked;
  protected bool Leap => ClockNode.Leaped;

  public override void _Ready() {
    clockNode_ = GetNode<ClockNode>("/root/Root/Clock");
    clock_ = clockNode_.Clock;
  }

  public override void _Process(double delta) {
  }
}
