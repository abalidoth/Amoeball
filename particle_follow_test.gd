extends GPUParticles2D


# Declare member variables here. Examples:
# var a = 2
# var b = "text"

var mat = get_process_material()
# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.

func _input(event):
	if event is InputEventMouseMotion:
		var new_pos = (event.position - position)
		mat.direction = Vector3(new_pos.x, new_pos.y, 0)
# Called every frame. 'delta' is the elapsed time since the previous frame.
#func _process(delta):
#	pass
