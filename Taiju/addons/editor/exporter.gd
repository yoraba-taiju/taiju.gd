@tool
extends EditorExportPlugin
class_name Exporter

func _export_begin(features: PackedStringArray, is_debug: bool, path: String, flags: int):
	var stage_compiler: StageCompiler = preload("res://Scenes/Compiler/stage_compiler.gd").new()
	stage_compiler.compile()
