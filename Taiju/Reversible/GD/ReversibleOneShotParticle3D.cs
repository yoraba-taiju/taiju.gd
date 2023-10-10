using System;
using Godot;

namespace Taiju.Reversible.GD;

// https://docs.godotengine.org/en/stable/tutorials/performance/vertex_animation/controlling_thousands_of_fish.html
public abstract partial class ReversibleOneShotParticle3D : Node3D {
  [Export] protected Mesh Mesh;
  [Export] protected int MeshCount = 16;
  [Export] protected float MaxSpeed = 10.0f;
  [Export(PropertyHint.Range, "1.0, 60.0")] protected double EmitPerSecond;

 
  // https://docs.godotengine.org/en/stable/classes/class_multimesh.html
  private MultiMeshInstance3D multiMesh_;
  private RandomNumberGenerator rand_;
  
  // MeshData
  private double bornAt_;
  protected MultiMesh Meshes { get; private set; }

  protected struct Item {
    public Color Color;
    public float Velocity;
    public Vector2 Angle;
    public float LifeTime;
  }

  // Storages
  private ClockNode clockNode_;
  private Item[] items_;

  public override void _Ready() {
    Meshes = new MultiMesh();
    clockNode_ = GetNode<ClockNode>("/root/Root/Clock");
    multiMesh_ = GetNode<MultiMeshInstance3D>("MultiMesh");
    Meshes.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
    Meshes.Mesh = Mesh;
    Meshes.UseColors = true;
    Meshes.InstanceCount = MeshCount;
    multiMesh_.Multimesh = Meshes;
    items_ = new Item[MeshCount];
    bornAt_ = clockNode_.IntegrateTime;
    _Emit(ref items_, 0.0);
  }

  public override void _Process(double dt) {
    var integrateTime = clockNode_.IntegrateTime - bornAt_;
    _Update(ref items_, integrateTime);
  }

  protected abstract void _Emit(ref Item[] items, double integrateTime);

  protected abstract void _Update(ref Item[] items, double integrateTime);
}

