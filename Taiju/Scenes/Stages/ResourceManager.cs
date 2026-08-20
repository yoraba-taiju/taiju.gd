#nullable enable
using System.Collections.Generic;
using Godot;

namespace Taiju.Scenes.Stages;

public class ResourceManager {
  private class Entry {
    public required Resource Resource;
    public required Node? NodeCache;
  }
  private readonly Dictionary<string, Entry> resourceCache_ = new();
  private static string NormalizePath(string path) {
    if (path.StartsWith("uid://")) {
      return ResourceUid.UidToPath(path);
    }

    return path;
  }
  public T Load<T>(string path) where T : Resource, new() {
    path = NormalizePath(path);
    if (resourceCache_.TryGetValue(path, out var value)) {
      return (T)value.Resource;
    }
    GD.PushWarning($"{path} does not exists.");
    var resource = ResourceLoader.Load<T>(path);
    Add(path, resource);
    return resource;
  }

  public void Add(string path, Resource resource) {
    path = NormalizePath(path);
    var entry = new Entry {
      Resource = resource,
      NodeCache = null,
    };
    // TODO: Arrow だけは先読みキャッシュを作らない。
    // 作ると終了時（× ボタン）に必ずこれが出る:
    //   WARNING: 1 ObjectDB instance was leaked at exit
    // 原因は未特定。有力な候補は ReversibleTubeTrail の
    //   private MeshInstance3D meshInstance_ = new();
    // で、これは Instantiate() の時点で親なしの Node を 1 個作り、AddChild は _Ready() の中。
    // ツリーに入らないキャッシュでは _Ready() が走らないので孤児のまま残り、Arrow 本体を
    // Free() しても子ではないので巻き添えにならない。漏れる数が 1 なのとも符合するし、
    // フィールド初期化子で Node を作っているのは ReversibleTubeTrail だけで、その派生は
    // Arrow だけ、というのも合う。ただし未検証。
    // これが原因なら _Ready() 生成に寄せるか NotificationPredelete で自前解放すれば
    // この条件は消せる。→ TODO.md
    if (resource is PackedScene scene && path != "res://Objects/Effect/Arrow.tscn") {
      entry.NodeCache = scene.Instantiate<Node>();
    }
    resourceCache_.Add(path, entry);
  }

  public T Instantiate<T>(string path) where T : Node, new() {
    path = NormalizePath(path);
    if (resourceCache_.TryGetValue(path, out var entry)) {
      var resource = (PackedScene)entry.Resource;
      entry.NodeCache ??= resource.Instantiate<T>();
      var node = (T)entry.NodeCache;
      entry.NodeCache = resource.Instantiate<T>();
      return node;
    }
    GD.PushWarning($"{path} does not exists in cache");
    var scene = Load<PackedScene>(path);
    var newNode = scene.Instantiate<T>();
    Add(path, scene);
    return newNode;
  }

  public void Free() {
    foreach (var entry in resourceCache_.Values) {
      entry.NodeCache?.Free();
    }
  }
  
  public int Count => resourceCache_.Count;
}
