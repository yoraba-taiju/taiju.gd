using System.Text.Json.Serialization;

namespace Taiju.Scenes.Model;

public class Stage {
  [JsonPropertyName("Events")]
  public Event[] Events { get; set; }
}
