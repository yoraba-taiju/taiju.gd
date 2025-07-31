using System.Text.Json.Serialization;

namespace Taiju.Scenes.Model.Events;

public class CurvePoint {
  public struct Point {
    [JsonPropertyName("X")] public required float X { get; init; }
    [JsonPropertyName("Y")] public required float Y { get; init; }
  }
  [JsonPropertyName("In")] public required Point In { get; init; }
  [JsonPropertyName("Out")] public required Point Out { get; init; }
  [JsonPropertyName("Position")] public required Point Position { get; init; }
}
