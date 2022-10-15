using System.ComponentModel;
using Godot;
using Godot.Collections;

namespace Taiju.Plugins.ShaderNodes;

[Tool]
public partial class PerlinNoise3D : VisualShaderNodeCustom {
  public PerlinNoise3D() {
    SetInputPortDefaultValue(2, 0.0);
  }
  public override string _GetName() {
    return "PerlinNoise3D";
  }

  public override string _GetCategory() {
    return "Noise";
  }
  
  public override string _GetDescription() {
    return "Classic Perlin-Noise-3D function (by Curly-Brace)";
  }

  public override long _GetReturnIconType() {
    return (long)PortType.Scalar;
  }

  public override long _GetInputPortCount() {
    return 4;
  }
  
  public override string _GetInputPortName(long port) {
    return port switch {
      0 => "UV",
      1 => "Offset",
      2 => "Scale",
      3 => "Time",
      _ => throw new InvalidEnumArgumentException()
    };
  }
  
  public override long _GetInputPortType(long port) {
    return (long) (port switch {
      0 => PortType.Vector2d,
      1 => PortType.Vector2d,
      2 => PortType.Scalar,
      3 => PortType.Scalar,
      _ => throw new InvalidEnumArgumentException()
    });
  }

  public override long _GetOutputPortCount() {
    return 1;
  }
  
  public override string _GetOutputPortName(long port) {
    return "Result";
  }
  
  public override long _GetOutputPortType(long port) {
    return (long)PortType.Scalar;
  }

  public override string _GetCode(Array<string> inputVars, Array<string> outputVars, Godot.Shader.Mode mode, VisualShader.Type type) {
    return $"{outputVars[0]} = cnoise(vec3(({inputVars[0]}.xy + {inputVars[1]}.xy) * {inputVars[2]}, {inputVars[3]})) * 0.5 + 0.5;";
  }

  public override string _GetGlobalCode(Godot.Shader.Mode mode) {
    return GD.Load<string>("res://Plugins/ShaderNodes/PerlinNoise3D.glsl");
  }
}
