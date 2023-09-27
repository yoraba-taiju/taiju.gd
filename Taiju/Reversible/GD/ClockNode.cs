using System.Collections.Generic;
using Godot;
using Taiju.Util;

namespace Taiju.Reversible.GD; 

public partial class ClockNode : Node3D {
  public Clock Clock { get; private set; }
  private Camera3D mainCamera_;
  private Viewport mainViewport_;

  /* GameObject Management */
  public HashSet<Node3D> LivingEnemies { get; } = new();
  private RingBuffer<(uint, Node3D)> deactivated_ = new(16384);

  public override void _Ready() {
    Clock = new Clock();
    mainCamera_ = GetNode<Camera3D>("/root/Root/MainCamera");
    mainViewport_ = mainCamera_.GetViewport();
  }

  public override void _Process(double delta) {
  }
}
