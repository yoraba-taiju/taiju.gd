using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Taiju.Scenes.Model;

public class Stage {
  [JsonPropertyName("Events")]
  public Event[] Events { get; set; }

  public Stage ForStager() {
    var events = new List<Event>();
    foreach (var ev in Events) {
      switch (ev) {
        case Model.Events.Preload:
          // ignore
          break;
        default:
          events.Add(ev);
          break;
      }
    }

    return new Stage {
      Events = events.ToArray(),
    };
  }
}
