extends Control

func _ready():
	%Title.play()


func _on_vs_local_pressed():
	get_tree().change_scene_to_file("res://VsLocal.tscn")


func _on_how_to_play_pressed():
	
	get_tree().change_scene_to_file("res://how_to_play.tscn")
