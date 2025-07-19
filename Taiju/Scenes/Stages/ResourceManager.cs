using System.Collections.Generic;
using Godot;

namespace Taiju.Scenes.Stages;

public class ResourceManager(
  Dictionary<string, Resource> resourceCache
) {
  public T Load<T>(string path) where T : Resource, new() {
    if (path.StartsWith("uid://")) {
      var uid = path;
      path = ResourceUid.UidToPath(uid);
    }
    if (resourceCache.TryGetValue(path, out var value)) {
      return (T)value;
    }
    GD.PushWarning($"{path} does not exists.");
    var resource = ResourceLoader.Load<T>(path);
    resourceCache.Add(path, resource);
    return resource;
  }

  public void Add(string path, Resource resource) {
    if (path.StartsWith("uid://")) {
      var uid = path;
      path = ResourceUid.UidToPath(uid);
      GD.Print($"{uid} ->  {path}");
    }
    resourceCache.Add(path, resource);
  }
  
  public int Count => resourceCache.Count;
}
