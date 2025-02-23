
extends ColorRect
class_name BlobShader

var current_move:Vector2
var current_move_type:String

var timer_time:float

func _ready() -> void:
	timer_time = $Timer.wait_time

func update_slimes(
	points:Array,
	length:Array,
	ball_pos:Vector2,
	move_pos:Vector2,
	move_type:String,
	current_player:int
) -> void:
	current_move = move_pos
	material.set_shader_parameter("ball_position", ball_pos)
	for player in range(2):
		var slime_color = ["green","purple"][player]
		material.set_shader_parameter(slime_color+"_slimes",points[player])
		material.set_shader_parameter("num_"+slime_color, length[player])
	$Timer.start()
	material.set_shader_parameter("progress", 0)
	material.set_shader_parameter("move_position", move_pos)
	var move_int
	match move_type:
		"place":
			move_int=0
		"remove":
			move_int=1
		"kick":
			move_int=2
	material.set_shader_parameter("move_type", move_int)
	material.set_shader_parameter("current_player", current_player)
	

func _process(delta:float) -> void:
	var progress = 1 - $Timer.time_left/timer_time
	if not $Timer.is_stopped():
		material.set_shader_parameter("progress", progress)
	else:
		material.set_shader_parameter("move_type",-1)
