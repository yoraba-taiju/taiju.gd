using System.Collections.Generic;
using Godot;
using Taiju.Util;

namespace Taiju.Reversible.GD; 

public partial class ClockNode : Node3D {
  public Clock Clock { get; private set; }
  public double IntegrateTime { get; private set; }
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
  }

  public override void _Process(double delta) {
    IntegrateTime += delta;
    leftToTick_ += delta;
    Ticked = false;
    Back = false;
    Leaped = false;
    
    if (Input.IsActionJustPressed("time_back")) {
      leftToTick_ = 0.0;
      Clock.Back();
      Back = true;
      return;
    }

    if (Input.IsActionJustPressed("time_back")) {
      leftToTick_ = 0.0;
      Clock.Leap();
      Leaped = true;
      return;
      
    }
    
    if (Input.IsActionPressed("time_back")) {
      Back = true;
      if (leftToTick_ > TickTime) {
        leftToTick_ -= TickTime;
        Clock.Back();
      }
      return;
    }

    Forward = true;

    if (leftToTick_ <= TickTime) {
      return;
    }

    leftToTick_ -= TickTime;
    Clock.Tick();
    Ticked = true;
  }
}
