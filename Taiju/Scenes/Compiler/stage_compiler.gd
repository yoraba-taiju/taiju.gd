@tool
class_name StageCompiler

const ScenePaths = [
	"res://Scenes/Stages/Stage01/Stage.tscn"
]

func compile():
	for i in range(0, len(ScenePaths)):
		compile_stage(ScenePaths[i])

func compile_stage(scene_path: String):
	var scene = load(scene_path).instantiate()
	var nodes: Array[Event] = []

	for node in scene.get_children():
		if node is Event:
			nodes.append(node)

	# Sort raws by X position
	nodes.sort_custom(func(a, b): return a.position.x < b.position.x)

	var events = []
	for node in nodes:
		if node is Rush:
			var spawns = []
			for child in node.get_children():
				spawns.append(compile_spawn(child as Spawn))
			events.append({
				"EventType": "Rush",
				"X": node.position.x,
				"Y": node.position.y,
				"Spawns": spawns
			})
		elif node is Spawn:
			events.append(compile_spawn(node as Spawn))
		elif node is Trigger:
			events.append({
				"EventType": "Trigger",
				"X": node.position.x,
				"Y": node.position.y,
				"Type": node.type,
			})
		elif node is Preload:
			events.append({
				"EventType": "Preload",
				"X": node.position.x,
				"Y": node.position.y,
				"Path": node.path,
			})
		else:
			push_error("Invalid node type encountered")
			return
	var stage = {
		"Events": events,
	};
	var stage_path = scene_path + ".json"
	var json = JSON.stringify(stage, "  ")
	var file = FileAccess.open(stage_path, FileAccess.WRITE)
	if file:
		file.store_string(json)
		file.close()
		print("Saved: ", stage_path)
	else:
		print("Failed to open file for writing")

func compile_spawn(node: Spawn):
	var obj = {
		"EventType": "Spawn",
		"X": node.position.x,
		"Y": node.position.y,
		"Path": node.path,
	}
	for child in node.get_children():
		if child is Path2D:
			var curves = []
			var curve = child.curve
			for idx in range(curve.point_count - 1, -1, -1):
				curves.append({
					"Position": compile_point(curve.get_point_position(idx)),
					# Reverse curve, so swap in and out.
					"In": compile_point(curve.get_point_out(idx)),
					"Out": compile_point(curve.get_point_in(idx)),
				})
			obj["Curve"] = curves
		else:
			printerr("Unkown child node type: %s" % child.get_class())
	return obj

func compile_point(v: Vector2):
	return {
		"X": v.x,
		"Y": v.y,
	}
