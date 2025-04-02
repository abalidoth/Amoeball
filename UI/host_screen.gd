extends Control

func _ready() -> void:
	$/root/MultiplayerConnector.server_started.connect(server_started)
	$/root/MultiplayerConnector.client_has_joined.connect(client_joined)
	
	
func server_started(room_id:String):
	%ServerID.text = room_id
	%CopyButton.disabled=false

func _on_create_button_pressed() -> void:
	$/root/MultiplayerConnector.initiate_server()


func _on_copy_button_pressed() -> void:
	DisplayServer.clipboard_set(%ServerID.text)

func client_joined() -> void:
	PlayerManager.left_player = "res://Agents/PlayerAgent.tscn"
	PlayerManager.right_player = "res://Agents/network_agent.tscn"
	get_tree().change_scene_to_file("res://GameManager.tscn")


func _on_back_button_pressed() -> void:
	get_tree().change_scene_to_file("res://UI/main_menu.tscn")
