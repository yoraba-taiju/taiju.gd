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
  public bool Backed { get; private set; }
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
    Backed = false;
    Leaped = false;
    if (leftToTick_ > TickTime) {
      leftToTick_ -= TickTime;
      Clock.Tick();
      Ticked = true;
    }
  }
}
