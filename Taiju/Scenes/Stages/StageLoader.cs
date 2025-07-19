using System;
using System.Collections.Generic;
using Godot;

namespace Taiju.Scenes.Stages;

public partial class StageLoader : Node2D {
  [Export(PropertyHint.File, "*.json")] private string stagePath_;
  [Export(PropertyHint.File, "*.tscn")] public string StageScenePath { get; private set; }
  public PackedScene StageScene { get; private set; }

  private static readonly string[] CommonPreloadScenes = [
    "res://Objects/Effect/Arrow.tscn",
    "res://Objects/Effect/MagicCircle.tscn",
    "res://Objects/Effect/MagicElementItem.tscn",
    "res://Objects/Effect/HitSparkle_SoraBullet.tscn",
    "res://Objects/Effect/StarDust.tscn",
    "res://Objects/Effect/ReversibleExplosion.tscn",
    "res://Objects/Effect/Spirit.tscn",
    "res://Objects/Witch/Sora.tscn",
    "res://Objects/Witch/SoraClone.tscn",
    "res://Objects/Witch/SoraBullet.tscn",
  ];

  private enum LoadState {
    NotLoaded,
    Loading,
    Loaded,
  }

  public Model.Stage Stage { get; private set; }
  public int Progress { get; private set; }
  private readonly List<string> preloadScenes_ = new();
  public readonly ResourceManager ResourceManager = new(new Dictionary<string, Resource>());
  private int currentSceneIndex_;
  private LoadState loadState_;
  private double nextPoll_ = 0.1;
  private double pollBackoff_ = 0.1;
  public bool Done { get; private set; }

  public override void _Ready() {
    base._Ready();
    Progress = 0;
    Stage = Model.StageDeserializer.Load(stagePath_)!;
    Done = false;
    currentSceneIndex_ = 0;
    loadState_ = LoadState.NotLoaded;
    var set = new HashSet<string>(CommonPreloadScenes);
    set.Add(StageScenePath);
    foreach (var ev in Stage.Events) {
      switch (ev) {
        case Model.Events.Spawn spawn:
          set.Add(spawn.Path);
          break;
        case Model.Events.Rush rush:
          foreach (var spawn in rush.Spawns) {
            set.Add(spawn.Path);
          }
          break;
        case Model.Events.Trigger:
          // ignore
          break;
        case Model.Events.Preload preload:
          set.Add(preload.Path);
          break;
        default:
          throw new InvalidCastException($"Unknown Type: {ev.GetType()}");
      }
    }
    preloadScenes_.AddRange(set);
  }
#if DEBUG
  public override void _Process(double delta) {
    base._Process(delta);
    if (Done) {
      return;
    }
    var path = preloadScenes_[currentSceneIndex_];
    var resource = ResourceLoader.Load(path);
    ResourceManager.Add(path, resource);
    currentSceneIndex_++;
    Progress = Mathf.RoundToInt(currentSceneIndex_ * 100.0f / preloadScenes_.Count);
    GD.Print($"Loaded: {path}");
    if (currentSceneIndex_ < preloadScenes_.Count) {
      // You can load resources more.
      return;
    }
    OnFinish();
    Done = true;
    loadState_ = LoadState.Loaded;
    GD.Print($"{currentSceneIndex_} Resources Loaded.");
  }
#else
  public override void _Process(double delta) {
    base._Process(delta);
    if (Done) {
      return;
    }

    if (currentSceneIndex_ >= preloadScenes_.Count) {
      OnFinish();
      Done = true;
      loadState_ = LoadState.Loaded;
      return;
    }

    nextPoll_ -= delta;
    if (nextPoll_ > 0.0) {
      return;
    }

    switch (loadState_) {
      case LoadState.NotLoaded: {
        pollBackoff_ = 0.1;
        nextPoll_ = pollBackoff_;

        var error = ResourceLoader.LoadThreadedRequest(preloadScenes_[currentSceneIndex_], "PackedScene", true);
        loadState_ = LoadState.Loading;
        if (error != Error.Ok) {
          GD.PrintErr($"Failed to start loading resource: {preloadScenes_[currentSceneIndex_]}, Error: {error}");
        }
      }
        break;
      case LoadState.Loading: {
        var path = preloadScenes_[currentSceneIndex_];
        var status = ResourceLoader.LoadThreadedGetStatus(path);
        switch (status)
        {
          case ResourceLoader.ThreadLoadStatus.InProgress:
            nextPoll_ += pollBackoff_;
            pollBackoff_ = Math.Min(1.0, pollBackoff_ * 2);
            break;
          case ResourceLoader.ThreadLoadStatus.Loaded: {
            nextPoll_ = 0;
            pollBackoff_ = 0.1;

            ResourceManager.Add(path, ResourceLoader.LoadThreadedGet(path));
            GD.Print($"Loaded: {path}");
            loadState_ = LoadState.Loaded;
            break;
          }
          case ResourceLoader.ThreadLoadStatus.Failed:
          case ResourceLoader.ThreadLoadStatus.InvalidResource:
            nextPoll_ = 0;
            pollBackoff_ = 0.1;

            GD.PrintErr($"Failed to load resource: {path}");
            loadState_ = LoadState.Loaded;
            break;
        }
      }
        break;
      case LoadState.Loaded:
        pollBackoff_ = 0.1;
        nextPoll_ = 0.0;
        currentSceneIndex_++;
        Progress = Mathf.RoundToInt(currentSceneIndex_ * 100.0f / preloadScenes_.Count);
        if (currentSceneIndex_ < preloadScenes_.Count) {
          loadState_ = LoadState.NotLoaded;
        } else {
          GD.Print($"{ResourceManager.Count} Resources Loaded.");
          OnFinish();
          Done = true;
          loadState_ = LoadState.Loaded;
        }
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }
#endif

  private void OnFinish() {
    StageScene = ResourceManager.Load<PackedScene>(StageScenePath);
    foreach (var ev in Stage.Events) {
      switch (ev) {
        case Model.Events.Spawn spawn:
          spawn.Scene = ResourceManager.Load<PackedScene>(spawn.Path);
          break;
        case Model.Events.Rush rush:
          foreach (var spawn in rush.Spawns) {
            spawn.Scene = (PackedScene)ResourceManager.Load<PackedScene>(spawn.Path);
          }
          break;
        case Model.Events.Trigger:
          // ignore.
          break;
        case Model.Events.Preload preload:
          preload.Scene = ResourceManager.Load<PackedScene>(preload.Path);
          break;
        default:
          throw new InvalidCastException($"Unknown Type: {ev.GetType()}");
      }
    }
  }
}
