using System;
using Godot;

namespace Taiju.Objects.Enemy.Drone4;

public partial class Kamikaze : Base<Kamikaze.Param> {
  [Export] private float initialSpeed_ = 10.0f;

  // Current State
  private float angle_ = 0.0f;

  // Param
  public enum State {
    Init,
  }
  public struct Param {
    public State State;
    public Vector3 Velocity;
    public Vector3 Position;
    public Vector3 Rotation;
  }

  // Nodes

  public override void _Ready() {
    base._Ready();
    ref var param = ref Record.Mut.Param;
    param.State = State.Init;
    angle_ = 0.0f;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var param = ref Record.Mut.Param;
    switch (param.State) {
      case State.Init: {
        
      }
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
    return base._ProcessForward(integrateTime, dt);
  }
}
