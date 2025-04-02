extends Control

func _ready() -> void:
	$/root/MultiplayerConnector.client_has_joined.connect(connection_established)
	$/root/MultiplayerConnector.client_has_left.connect(connection_broken)


func _on_join_button_pressed() -> void:
	$/root/MultiplayerConnector.connect_to_server(%RoomID.text)


func _on_room_id_text_submitted(new_text: String) -> void:
	$/root/MultiplayerConnector.connect_to_server(%RoomID.text)

func _on_room_id_text_changed(new_text: String) -> void:
	%JoinButton.disabled = (new_text == "")
	
func connection_established():
	PlayerManager.left_player = "res://Agents/network_agent.tscn"
	PlayerManager.right_player = "res://Agents/PlayerAgent.tscn"
	get_tree().change_scene_to_file("res://GameManager.tscn")
	
func connection_broken():
	pass


func _on_back_button_pressed() -> void:
	get_tree().change_scene_to_file("res://UI/main_menu.tscn")
