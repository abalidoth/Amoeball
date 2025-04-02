extends Control

func _ready():
	%Title.play()


func _on_vs_local_pressed():
	PlayerManager.left_player = "res://Agents/PlayerAgent.tscn"
	PlayerManager.right_player = "res://Agents/PlayerAgent.tscn"
	get_tree().change_scene_to_file("res://GameManager.tscn")
	



func _on_how_to_play_pressed():
	
	get_tree().change_scene_to_file("res://how_to_play/how_to_play.tscn")


func _on_vs_ai_pressed():
	
	PlayerManager.left_player = "res://Agents/PlayerAgent.tscn"
	PlayerManager.right_player = "res://Agents/ai_agent.tscn"
	get_tree().change_scene_to_file("res://GameManager.tscn")


func _on_host_button_pressed():
	get_tree().change_scene_to_file("res://UI/host_screen.tscn")
	



func _on_join_button_pressed():
	get_tree().change_scene_to_file("res://UI/join_screen.tscn")
