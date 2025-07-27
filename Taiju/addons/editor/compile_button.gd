extends Button
class_name CompileButton

func _ready():
	text = "⛏️ Compile Stages"
	pressed.connect(_on_pressed)

func _on_pressed():
	EditorInterface.save_scene()
	var stage_compiler = preload("res://Scenes/Compiler/stage_compiler.gd").new()
	stage_compiler.compile()
