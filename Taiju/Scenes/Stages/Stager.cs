using System.IO;
using Godot;
using Taiju.Objects;
using Taiju.Objects.Witch;
using Taiju.Util.Godot;
using Taiju.Util.Reversible.Godot;
using Taiju.Util.Reversible.Value;

namespace Taiju.Scenes.Stages;

public abstract partial class Stager : ReversibleNode3D {
  // const
  private const float StageWidth = 1280.0f;
  private const float StageHeight = 720.0f;
  private const double StageSpeed = 120.0;

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
      while (stageIndex < Stage.Events.Length && Stage.Events[stageIndex].X < invokePosition) {
        InvokeEvent(stagePosition, Stage.Events[stageIndex]);
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

  private void InvokeEvent(double stagePosition, Model.Event ev) {
    switch (ev) {
      case Model.Events.Rush rush:
        OnRush(stagePosition, rush);
        break;
      case Model.Events.Spawn spawn:
        OnSpawn(stagePosition, spawn);
        break;
      case Model.Events.Trigger trigger:
        OnTrigger(stagePosition, trigger);
        break;
      case Model.Events.Preload:
        break;
    }
  }

  private void OnSpawn(double stagePosition, Model.Events.Spawn spawn) {
    var node = ResourceManager.Load<PackedScene>(spawn.Path).Instantiate<Node3D>();
    node.Position = ScreenToWorld(new Vector2((float)(spawn.X - stagePosition), spawn.Y));
    DefaultRush.AddChild(node);
  }

  private void OnRush(double stagePosition, Model.Events.Rush rush) {
    var rushNode = new Objects.Rush.Rush();
    Enemy.AddChild(rushNode);
    var basePos = new Vector2((float)(rush.X - stagePosition), rush.Y);
    foreach (var spawn in rush.Spawns) {
      var node = ResourceManager.Load<PackedScene>(spawn.Path).Instantiate<Node3D>();
      rushNode.AddChild(node);
      var pos = new Vector2(spawn.X, spawn.Y) + basePos;
      node.Position = ScreenToWorld(pos);
    }
  }

  protected abstract void OnTrigger(double stagePosition, Model.Events.Trigger trigger);

  private Vector3 ScreenToWorld(Vector2 screen) {
    var rayOrigin = MainCamera.ProjectRayOrigin(screen);
    var rayNormal = MainCamera.ProjectRayNormal(screen);
    if (Mathf.IsZeroApprox(rayNormal.Z)) {
      throw new InvalidDataException("Ray is parallel to Z=0 plane");
    }

    float t = -rayOrigin.Z / rayNormal.Z;
    return rayOrigin + rayNormal * t;
  }
}
