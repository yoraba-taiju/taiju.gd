using Godot;

namespace Taiju.Util.Reversible.Godot;

public partial class ClockNode : Node3D {
  public Clock Clock { get; private set; }
  private double leftToTick_;
  private const double TickTime = Clock.TickTime;
  
  private struct Grave {
    public uint DestroyedAt;
    public Node Node;
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
      if (Clock.Back()) {
        ProcessRescue();
      }
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
        if (Clock.Back()) {
          ProcessRescue();
        }
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
