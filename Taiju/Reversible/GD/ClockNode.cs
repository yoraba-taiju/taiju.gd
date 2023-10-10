using System.Collections.Generic;
using Godot;
using Taiju.Reversible.Value;
using Taiju.Util;

namespace Taiju.Reversible.GD; 

public partial class ClockNode : Node3D {
  public Clock Clock { get; private set; }
  public double IntegrateTime => integrateTime_.Ref;
  private Dense<double> integrateTime_;
  private double leftToTick_ = 0.0;
  private const double TickTime = 1.0 / 30.0;

  public bool Ticked { get; private set; }
  public bool Back { get; private set; }
  public bool Forward { get; private set; }
  public bool Leaped { get; private set; }

  /* GameObject Management */
  public HashSet<Node3D> LivingEnemies { get; } = new();
  private RingBuffer<(uint, Node3D)> deactivated_ = new(16384);

  public override void _Ready() {
    Clock = new Clock();
    integrateTime_ = new Dense<double>(Clock, 0.0);
  }

  public override void _Process(double delta) {
    ref var integrateTime = ref integrateTime_.Mut;
    leftToTick_ += delta;
    Ticked = false;
    Back = false;
    Leaped = false;

    // Backing started
    if (Input.IsActionJustPressed("time_back")) {
      leftToTick_ = 0.0;
      Clock.Back();
      Back = true;
      return;
    }

    // Leaped
    if (Input.IsActionJustPressed("time_back")) {
      leftToTick_ = 0.0;
      Clock.Leap();
      Leaped = true;
      return;
    }

    // Backing
    if (Input.IsActionPressed("time_back")) {
      Back = true;
      if (leftToTick_ > TickTime) {
        leftToTick_ -= TickTime;
        Clock.Back();
        Ticked = true;
      }
      return;
    }

    // Forwarding
    Forward = true;
    integrateTime += delta;
    if (leftToTick_ > TickTime) {
      leftToTick_ -= TickTime;
      Clock.Tick();
      Ticked = true;
    }
  }
}
