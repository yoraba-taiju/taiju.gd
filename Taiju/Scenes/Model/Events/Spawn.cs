#nullable enable
using System.Text.Json.Serialization;
using Godot;

namespace Taiju.Scenes.Model.Events;

public class Spawn : Event {
  [JsonPropertyName("Path")]
  public required string Path { get; init; }
  [JsonPropertyName("Curve")]
  public CurvePoint[]? Curve { get; init; }
}
