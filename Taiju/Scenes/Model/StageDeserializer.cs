using System.Text.Json;
using Godot;
using Taiju.Scenes.Model.JsonConverters;

namespace Taiju.Scenes.Model;

public static class StageDeserializer {
  public static Model.Stage Load(string jsonPath) {
    var options = new JsonSerializerOptions {
      Converters = {
        // new JsonConverters.StageConverter(),
        new EventConverter(),
      },
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    using var fs = FileAccess.Open(jsonPath, FileAccess.ModeFlags.Read);
    var json = fs.GetAsText();
    return JsonSerializer.Deserialize<Model.Stage>(json, options);
  }
}
