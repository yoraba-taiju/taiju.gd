@tool
class_name VisualShaderNodePerlinNoise3D
extends VisualShaderNodeCustom


func _get_name() -> String:
	return "PerlinNoise3D_GD"


func _get_category() -> String:
	return "Noise"

func _get_description() -> String :
	return "Classic Perlin-Noise-3D function (by Curly-Brace)"

func _init():
	set_input_port_default_value(2, 0.0)

func _get_return_icon_type():
	return VisualShaderNode.PORT_TYPE_SCALAR

func _get_input_port_count():
	return 4

func _get_input_port_name(port: int):
	match port:
		0:
			return "uv"
		1:
			return "offset"
		2:
			return "scale"
		3:
			return "time"

func _get_input_port_type(port: int):
	match port:
		0:
			return VisualShaderNode.PORT_TYPE_VECTOR
		1:
			return VisualShaderNode.PORT_TYPE_VECTOR
		2:
			return VisualShaderNode.PORT_TYPE_SCALAR
		3:
			return VisualShaderNode.PORT_TYPE_SCALAR

func _get_output_port_count():
	return 1


func _get_output_port_name(port):
	return "result"


func _get_output_port_type(port):
	return VisualShaderNode.PORT_TYPE_SCALAR


func _get_global_code(mode):
	return preload("res://Plugins/ShaderNodes/PerlinNoise3D.glsl");


func _get_code(input_vars, output_vars, mode, type):
	return output_vars[0] + " = cnoise(vec3((%s.xy + %s.xy) * %s, %s)) * 0.5 + 0.5;" % [input_vars[0], input_vars[1], input_vars[2], input_vars[3]]
