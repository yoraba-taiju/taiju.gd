#nullable enable
using System.Text.Json.Serialization;
using Godot;

namespace Taiju.Scenes.Model.Events;

public class Spawn : Event {
  [JsonPropertyName("Path")]
  public required string Path { get; init; }
  [JsonPropertyName("Curve")]
  public CurvePoint[]? Curve { get; init; }

  public PackedScene? Scene { get; set; }
  public Node? NodeCache { get; set; }

  public T Instantiate<T>() where T : Node, new() {
    if (NodeCache == null) {
      return Scene!.Instantiate<T>();
    }
    var node = NodeCache;
    NodeCache = null;
    return (T)node;
  }
}
