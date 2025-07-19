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
  private readonly List<string> preloadScenes_ = new();
  private readonly Dictionary<string, Resource> resourceCache_ =  new();
  private int currentSceneIndex_;
  private LoadState loadState_;
  private Godot.Collections.Array progressArray_ = new([0]);
  public bool Done { get; private set; }

  public override void _Ready() {
    base._Ready();
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
    resourceCache_.Add(path, resource);
    currentSceneIndex_++;
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

    switch (loadState_) {
      case LoadState.NotLoaded: {
        var error = ResourceLoader.LoadThreadedRequest(preloadScenes_[currentSceneIndex_], "PackedScene", true);
        loadState_ = LoadState.Loading;
        if (error != Error.Ok) {
          GD.PrintErr($"Failed to start loading resource: {preloadScenes_[currentSceneIndex_]}, Error: {error}");
        }
      }
        break;
      case LoadState.Loading: {
        var path = preloadScenes_[currentSceneIndex_];
        var status = ResourceLoader.LoadThreadedGetStatus(path, progressArray_);
        switch (status)
        {
          case ResourceLoader.ThreadLoadStatus.InProgress:
            break;
          case ResourceLoader.ThreadLoadStatus.Loaded: {
            resourceCache_[path] = ResourceLoader.LoadThreadedGet(path);
            GD.Print($"Loaded: {path}");
            loadState_ = LoadState.Loaded;
            break;
          }
          case ResourceLoader.ThreadLoadStatus.Failed:
          case ResourceLoader.ThreadLoadStatus.InvalidResource:
            GD.PrintErr($"Failed to load resource: {path}");
            loadState_ = LoadState.Loaded;
            break;
        }
      }
        break;
      case LoadState.Loaded:
        currentSceneIndex_++;
        if (currentSceneIndex_ < preloadScenes_.Count) {
          loadState_ = LoadState.NotLoaded;
        } else {
          GD.Print($"{resourceCache_.Count} Resources Loaded.");
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
    StageScene = (PackedScene)resourceCache_[StageScenePath];
    foreach (var ev in Stage.Events) {
      switch (ev) {
        case Model.Events.Spawn spawn:
          spawn.Scene = (PackedScene)resourceCache_[spawn.Path];
          break;
        case Model.Events.Rush rush:
          foreach (var spawn in rush.Spawns) {
            spawn.Scene = (PackedScene)resourceCache_[spawn.Path];
          }
          break;
        case Model.Events.Trigger:
          // ignore.
          break;
        case Model.Events.Preload preload:
          preload.Scene = (PackedScene)resourceCache_[preload.Path];
          break;
        default:
          throw new InvalidCastException($"Unknown Type: {ev.GetType()}");
      }
    }
  }
}
