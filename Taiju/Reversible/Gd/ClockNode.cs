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
  
  private struct Grave {
    public uint DestroyedAt;
    public Node3D Node;
  }

  private RingBuffer<Grave> graveyard_ = new(16384);

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
      ProcessRescue();
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
      ProcessRescue();
      return;
    }

    // Forwarding
    Direction = TimeDirection.Forward;
    if (leftToTick_ <= 0.0) {
      leftToTick_ += TickTime;
      Clock.Tick();
      Ticked = true;
    }
    ProcessDestroy();
    ref var integrateTime = ref integrateTime_.Mut;
    integrateTime += delta;
  }

  private void ProcessDestroy() {
    while (!graveyard_.IsEmpty) {
      ref readonly var it = ref graveyard_.First;
      if (it.DestroyedAt + Clock.HistoryLength < Clock.CurrentTick) {
        it.Node.QueueFree(); // Vanish
        graveyard_.RemoveFirst();
      } else {
        break;
      }
    }
  }

  private void ProcessRescue() {
    while (!graveyard_.IsEmpty) {
      ref readonly var it = ref graveyard_.Last;
      if (it.DestroyedAt < Clock.CurrentTick) {
        (it.Node as IReversibleNode)?.Rescue();
        graveyard_.RemoveLast();
      } else {
        break;
      }
    }
  }

  public void Destroy(Node3D node) {
    graveyard_.AddLast(new Grave
    {
      DestroyedAt = Clock.CurrentTick,
      Node = node,
    });
  }
}
