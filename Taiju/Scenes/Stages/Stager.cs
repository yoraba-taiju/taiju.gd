using System.IO;
using Godot;
using Taiju.Objects;
using Taiju.Objects.Enemy;
using Taiju.Objects.Witch;
using Taiju.Util.Godot;
using Taiju.Util.Reversible.Godot;
using Taiju.Util.Reversible.Value;

namespace Taiju.Scenes.Stages;

public abstract partial class Stager : ReversibleNode3D {
  // const
  private const float StageWidth = 1280.0f;
  private const float StageHeight = 720.0f;
  private readonly Vector2 StageSize = new(StageWidth, StageHeight);
  private const double StageSpeed = 120.0;
  
  // Current status
  private Vector2 viewportSize_ = Vector2.Zero;

  // Record
  private struct Record {
    public bool TransformChanged;
    public Transform3D Transform;
    public int StageIndex;
    public double StagePosition;
  }
  private Dense<Record> record_;
  

  // Nodes
  protected Camera MainCamera { get; private set; }
  protected Sora Sora { get; private set; }
  protected Player Player { get; private set; }
  protected Node3D Enemy { get; private set; }
  protected Node3D DefaultRush { get; private set; }

  // Stage
  internal Model.Stage Stage { get; set; }
  public ResourceManager ResourceManager { get; internal set; }

  public override void _Ready() {
    base._Ready();
    record_ = new Dense<Record>(Clock, new Record {
      TransformChanged = false,
      Transform = Transform3D.Identity,
      StageIndex = 0,
      StagePosition = 0,
    });
    MainCamera = GetNode<Camera>("/root/Root/MainCamera")!;
    Sora = GetNode<Sora>("/root/Root/Field/Witch/Sora")!;
    Enemy = GetNode<Node3D>("/root/Root/Field/Enemy")!;
    DefaultRush = GetNode<Node3D>("/root/Root/Field/Enemy/DefaultRush")!;
    OnViewportSizeChanged();
  }

  public override void _EnterTree() {
    base._EnterTree();
    GetViewport().SizeChanged += OnViewportSizeChanged;
  }

  public override void _ExitTree() {
    base._ExitTree();
    GetViewport().SizeChanged -= OnViewportSizeChanged;
  }

  private void OnViewportSizeChanged() {
    var viewport = GetViewport();
    var rect = viewport.GetVisibleRect();
    var viewportSize = rect.Size;
    viewportSize_ = viewportSize;
  }

  protected void Move(Vector3 delta, double dt) {
    ref var rec = ref record_.Mut;
    rec.Transform = rec.Transform.TranslatedLocal(delta * (float)dt);
    rec.TransformChanged = true;
  }

  public override bool _ProcessForward(double integrateTime, double dt) {
    ref var rec = ref record_.Mut;
    if (rec.TransformChanged) {
      Transform = rec.Transform.Inverse();
      rec.TransformChanged = false;
    }

    { // ステージ進行
      ref var stagePosition = ref rec.StagePosition;
      stagePosition += StageSpeed * dt;
      var invokePosition = stagePosition + StageWidth;
      ref var stageIndex = ref rec.StageIndex;
      var basePos = new Vector2((float)-stagePosition, 0.0f);
      while (stageIndex < Stage.Events.Length && Stage.Events[stageIndex].X < invokePosition) {
        InvokeEvent(basePos, Stage.Events[stageIndex]);
        stageIndex++;
      }
    }
    return true;
  }

  public override bool _ProcessLeap(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    Transform = rec.Transform.Inverse();
    return true;
  }

  public override bool _ProcessBack(double integrateTime) {
    ref readonly var rec = ref record_.Ref;
    Transform = rec.Transform.Inverse();
    return true;
  }

  private void InvokeEvent(Vector2 basePosition, Model.Event ev) {
    switch (ev) {
      case Model.Events.Rush rush:
        OnRush(basePosition, rush);
        break;
      case Model.Events.Spawn spawn:
        OnSpawn(basePosition, spawn);
        break;
      case Model.Events.Trigger trigger:
        OnTrigger(basePosition, trigger);
        break;
      case Model.Events.Preload:
        break;
    }
  }

  private void OnSpawn(Vector2 basePosition, Model.Events.Spawn spawn) {
    DefaultRush.AddChild(LoadSpawn(spawn, basePosition));
  }

  private void OnRush(Vector2 basePosition, Model.Events.Rush rush) {
    var rushNode = new Objects.Rush.Rush();
    Enemy.AddChild(rushNode);
    var rushBasePos = basePosition + new Vector2(rush.X, rush.Y);
    foreach (var spawn in rush.Spawns) {
      rushNode.AddChild(LoadSpawn(spawn, rushBasePos));
    }
  }

  protected abstract void OnTrigger(Vector2 stagePosition, Model.Events.Trigger trigger);

  private Node3D LoadSpawn(Model.Events.Spawn spawn, Vector2 basePos) {
    var node = ResourceManager.Load<PackedScene>(spawn.Path).Instantiate<Node3D>();
    var pos = new Vector2(spawn.X, spawn.Y) + basePos;
    // Set position **before** AddChild.
    node.Position = ScreenToWorld(pos);
    if (spawn.Curve != null) {
      var curvePoints = spawn.Curve;
      var curve = new Curve3D();
      var halfStageSize = StageSize / 2;
      foreach (var point in curvePoints) {
        var position2d = new Vector2(point.Position.X, point.Position.Y);
        var position3d = ScreenToWorld(pos + position2d);
        var in3d = ScreenToWorld(new Vector2(point.In.X, point.In.Y) + halfStageSize);
        var out3d = ScreenToWorld(new Vector2(point.Out.X, point.Out.Y) + halfStageSize);
        curve.AddPoint(
          position3d,
          in3d,
          out3d
        );
      }

      if (node is IEnemyWithCurve enemy) {
        enemy.Curve = curve;
      } else {
        GD.PrintErr($"Curve is set, but node is not have a curve: {node.GetType().FullName}");
      }
    }
    return node;
  }

  private Vector3 ScreenToWorld(Vector2 stage) {
    var viewport = stage * viewportSize_ / StageSize;
    var rayOrigin = MainCamera.ProjectRayOrigin(viewport);
    var rayNormal = MainCamera.ProjectRayNormal(viewport);
    if (Mathf.IsZeroApprox(rayNormal.Z)) {
      throw new InvalidDataException("Ray is parallel to Z=0 plane");
    }

    var t = -rayOrigin.Z / rayNormal.Z;
    var pos = rayOrigin + rayNormal * t;
    pos.Z = 0.0f;
    return pos;
  }
}
