using System;
using System.Collections.Generic;
using Godot;
using Taiju.Reversible.Value;
using Taiju.Util;

namespace Taiju.Reversible.Gd; 

public partial class ClockNode : Node3D {
  public Clock Clock { get; private set; }
  public double IntegrateTime => integrateTime_.Ref;
  private Dense<double> integrateTime_;
  private double leftToTick_ = 0.0;
  public const double TickTime = 1.0 / 30.0;

  public enum TimeDirection {
    Stop,
    Forward,
    Back,
  }
  public TimeDirection Direction { get; private set; }
  public bool Ticked { get; private set; }
  public bool Leaped { get; private set; }

  public override void _Ready() {
    Clock = new Clock();
    integrateTime_ = new Dense<double>(Clock, 0.0);
    leftToTick_ = 0.0;
  }

  public override void _Process(double delta) {
    leftToTick_ -= delta;
    Direction = TimeDirection.Stop;
    Ticked = false;
    Leaped = false;

    // Backing started
    if (Input.IsActionJustPressed("time_back")) {
      Direction = TimeDirection.Back;
      leftToTick_ = 0.0;
      Clock.Back();
      return;
    }

    // Leaped
    if (Input.IsActionJustReleased("time_back")) {
      Direction = TimeDirection.Stop;
      leftToTick_ = 0.0;
      Clock.Leap();
      Leaped = true;
      return;
    }

    // Backing
    if (Input.IsActionPressed("time_back")) {
      Direction = TimeDirection.Back;
      if (leftToTick_ <= 0.0) {
        leftToTick_ += TickTime;
        Clock.Back();
        Ticked = true;
      }
      return;
    }

    // Forwarding
    Direction = TimeDirection.Forward;
    if (leftToTick_ <= 0.0) {
      leftToTick_ += TickTime;
      Clock.Tick();
      Ticked = true;
    }
    ref var integrateTime = ref integrateTime_.Mut;
    integrateTime += delta;
  }
}
