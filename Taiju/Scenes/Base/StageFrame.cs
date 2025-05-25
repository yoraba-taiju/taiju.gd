using Godot;
using Camera = Taiju.Util.Godot.Camera;

namespace Taiju.Scenes.Base;

public partial class StageFrame : Node2D {
  private Node3D enemyNode_;
  private Node3D defaultRush_;
  private Camera camera_;
  private Vector2 HalfScreenSize => camera_.HalfScreenSize;
  private Line2D area_;
  private Vector2 areaSize_;
  private Area2D hitArea_;
  public override void _Ready() {
    enemyNode_ = GetNode<Node3D>("/root/Root/Field/Enemy")!;
    defaultRush_ = GetNode<Node3D>("/root/Root/Field/Enemy/DefaultRush")!;
    camera_ = GetNode<Camera>("/root/Root/MainCamera")!;
    area_ = GetNode<Line2D>("SceneShape")!;
    hitArea_ = GetNode<Area2D>("Region")!;

    // Prepare size
    var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
    var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
    foreach (var pt in area_.Points) {
      min.X = Mathf.Min(min.X, pt.X);
      min.Y = Mathf.Min(min.Y, pt.Y);
      max.X = Mathf.Max(max.X, pt.X);
      max.Y = Mathf.Max(max.Y, pt.Y);
    }
    areaSize_ = max - min;
    // Prepare signal
    hitArea_.AreaEntered += HitAreaEntered;
  }

  private void HitAreaEntered(Area2D area) {
    switch (area) {
      case Spawn spawn:
        if (spawn.GetParentOrNull<Rush>() == null) {
          Instantiate(spawn, defaultRush_);
        }
        break;
      case Rush rush:
        Instantiate(rush);
        break;
      case Signal signal:
        if (signal.Path.IsEmpty) {
          break;
        }
        break;
      default:
        GD.PrintErr($"Maybe wrong node: {area.Name}/{area} (class = {area.GetClass()})");
        break;
    }
  }

  private void Instantiate(Spawn spawn, Node3D destination) {
    var transInverse = Transform.Inverse();
    var rush = spawn.GetParentOrNull<Rush>();
    var trans =
      rush == null ?
        transInverse * spawn.Transform :
        transInverse * rush.Transform * spawn.Transform;
    var enemy = spawn.Instantiate();

    // Position
    var position = trans.Origin;
    position = (position / areaSize_ * HalfScreenSize * 2.0f) - HalfScreenSize;
    enemy.Position = new Vector3(position.X, -position.Y, 0);

    // Scale
    var scale = trans.Scale;
    enemy.Scale = new Vector3(scale.X, scale.Y, 1.0f);

    // Rotation
    var rot = trans.Rotation;
    enemy.Rotation = new Vector3(0, 0, -rot);

    destination.AddChild(enemy);
  }
  
  private void Instantiate(Rush rush) {
    var rushBase = rush.Instantiate();
    rushBase.Name = Name;
    foreach (var node in rush.GetChildren()) {
      switch (node) {
        case Spawn spawn:
          Instantiate(spawn, rushBase);
          break;

        case CollisionShape2D shape when shape.Name == "Shape": // Skip shape
          break;

        default:
          GD.PrintErr($"Unknown object: {node.Name}/{node} (class = {node.GetClass()})");
          break;
      }
    }
    enemyNode_.AddChild(rushBase);
  }
}
