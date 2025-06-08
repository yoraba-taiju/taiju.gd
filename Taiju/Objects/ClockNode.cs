using System;
using Godot;
using Taiju.Util;
using Taiju.Util.Reversible;
using Taiju.Util.Reversible.Godot;

namespace Taiju.Objects;

public partial class ClockNode : Node3D {
  public Clock Clock { get; private set; }
  private double leftToTick_;
  private const double TickTime = Clock.TickTime;
  // Player
  private Player player_;
  
  private struct Grave {
    public uint DestroyedAt;
    public Node Node;
  }
  private RingBuffer<Grave> graveyard_ = new(16384);

  public enum TimeDirection {
    Forward,
    Back,
    Stop,
  }
  public TimeDirection Direction { get; private set; }
  public bool Leaped { get; private set; }

  public override void _Ready() {
    Clock = new Clock();
    player_ = GetNode<Player>("/root/Root/Player")!;
    leftToTick_ = 0.0;
  }

  public override void _Process(double delta) {
    leftToTick_ -= delta;
    Direction = TimeDirection.Stop;
    Leaped = false;

    switch (player_.CurrentClockOperation) {
      case Player.ClockOperation.StartForward:
      case Player.ClockOperation.Forward: {
        Direction = TimeDirection.Forward;
        if (leftToTick_ <= 0.0) {
          leftToTick_ += TickTime;
          Clock.Tick();
          player_.OnActionByClock(Player.ClockAction.ForwardTick);
        }
      }
        break;
      case Player.ClockOperation.StartBack: {
        Direction = TimeDirection.Back;
        leftToTick_ = 0.0;
        if (Clock.Back()) {
          player_.OnActionByClock(Player.ClockAction.BackTick);
          ProcessRescue();
        } else {
          Direction = TimeDirection.Stop;
        }
      }
        break;
      case Player.ClockOperation.Back:
        Direction = TimeDirection.Back;
        if (leftToTick_ <= 0.0) {
          leftToTick_ += TickTime;
          if (Clock.Back()) {
            player_.OnActionByClock(Player.ClockAction.BackTick);
            ProcessRescue();
          } else {
            Direction = TimeDirection.Stop;
          }
        }
        break;
      case Player.ClockOperation.Leap: {
        Direction = TimeDirection.Stop;
        leftToTick_ = 0.0;
        Clock.Leap();
        Leaped = true;
      }
        break;
      case Player.ClockOperation.Stop: {
        Direction = TimeDirection.Stop;
      }
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
    ProcessDestroy();
  }

  private void ProcessDestroy() {
    while (!graveyard_.IsEmpty) {
      ref readonly var it = ref graveyard_.First;
      if (it.DestroyedAt + Clock.HistoryLength < Clock.CurrentTick) {
        // Console.WriteLine($"Destroyed: {it.Node} ({it.Node.Name})");
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
      if (it.DestroyedAt >= Clock.CurrentTick) {
        var rev = (IReversibleNode)it.Node;
        rev.Rescue();
        graveyard_.RemoveLast();
      } else {
        break;
      }
    }
  }

  public void QueueDestroy(Node node) {
    graveyard_.AddLast(new Grave {
      DestroyedAt = Clock.CurrentTick,
      Node = node,
    });
  }
}
