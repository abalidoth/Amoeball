@tool
extends ColorRect

func update_slimes(points:Array, length:Array):
	for player in range(2):
		var slime_color = ["green","purple"][player]
		material.set_shader_parameter(slime_color+"_slimes",points[player])
		material.set_shader_parameter("num_"+slime_color, length[player])
