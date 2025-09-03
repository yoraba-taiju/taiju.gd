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
  private string NormalizePath(string path) {
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
    if (resource is PackedScene scene) {
      entry.NodeCache = scene.Instantiate<Node>();
    }
    resourceCache_.Add(path, entry);
  }

  public T Instantiate<T>(string path) where T : Node, new() {
    path = NormalizePath(path);
    if (resourceCache_.TryGetValue(path, out var entry)) {
      entry.NodeCache ??= ((PackedScene)entry.Resource).Instantiate<T>();
      var node = (T)entry.NodeCache;
      entry.NodeCache = ((PackedScene)entry.Resource).Instantiate<T>();
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
