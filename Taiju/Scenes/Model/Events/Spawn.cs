using System.Text.Json.Serialization;
using Godot;

namespace Taiju.Scenes.Model.Events;

public class Spawn : Event {
  [JsonPropertyName("Path")]
  public string Path { get; set; }

  public PackedScene Scene;
}
