@tool
extends EditorPlugin
class_name Plugin

var exporter = preload("res://addons/editor/exporter.gd").new()
var compile_button = preload("res://addons/editor/compile_button.gd").new()

func _enter_tree():
	add_export_plugin(exporter)
	add_control_to_container(CustomControlContainer.CONTAINER_CANVAS_EDITOR_MENU, compile_button)

func _exit_tree():
	compile_button.queue_free()
	remove_control_from_container(CustomControlContainer.CONTAINER_CANVAS_EDITOR_MENU, compile_button)
	remove_export_plugin(exporter)
