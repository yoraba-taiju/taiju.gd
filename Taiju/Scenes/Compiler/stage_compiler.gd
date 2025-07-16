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
	var raws = []
	for node in scene.get_children():
		if node is Rush:
			raws.append(node)
	
	# Sort raws by X position
	raws.sort_custom(func(a, b): return a.position.x < b.position.x)
	
	var events = []
	for raw in raws:
		if raw is Rush:
			var spawns = []
			for child in raw.get_children():
				var spawn = child as Spawn
				spawns.append({
					"Type": "Spawn",
					"X": spawn.position.x,
					"Y": spawn.position.y,
					"Path": spawn.enemy
				})
			events.append({
				"Type": "Rush",
				"X": raw.position.x,
				"Y": raw.position.y,
				"Spawns": spawns
			})
		elif raw is Spawn:
			events.append({
				"Type": "Spawn",
				"X": raw.position.x,
				"Y": raw.position.y,
				"Path": raw.enemy
			})
		elif raw is Signal:
			events.append({
				"Type": "Signal",
				"X": raw.position.x,
				"Y": raw.position.y,
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
