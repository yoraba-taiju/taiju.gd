using System.Text.Json;
using Godot;

namespace Taiju.Scenes.Loader;

public static class StageLoader {
  public static Model.Stage Load(string jsonPath) {
    var options = new JsonSerializerOptions {
      Converters = {
        // new JsonConverters.StageConverter(),
        new JsonConverters.EventConverter(),
      },
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    using var fs = FileAccess.Open(jsonPath, FileAccess.ModeFlags.Read);
    var json = fs.GetAsText();
    return JsonSerializer.Deserialize<Model.Stage>(json, options);
  }
}
