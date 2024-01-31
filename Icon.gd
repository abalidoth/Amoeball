extends Sprite2D

func _input(event):
	if event is InputEventMouseMotion:
		position = get_global_mouse_position()
