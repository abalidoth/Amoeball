extends AbstractAgent

class_name NetworkAgent

func _handle_game_state_change(new_state, new_player, game):
	print("received state change signal")
	var last_move = game.last_move
	#make sure the move was not this player
	if new_player != player or (new_player == player and new_state==GameState.STATE_PLACE_1):
		$/root/MultiplayerConnector.send_move(var_to_str(last_move))
	
func _setup_agent_specific():
	$/root/MultiplayerConnector.heard_move.connect(_on_heard_move)
	$/root/MultiplayerConnector.client_has_left.connect(_on_client_left)
	
func _on_heard_move(move):
	print("heard move", move)
	game_board.make_move(str_to_var(move))
	
func _on_client_left():
	%DisconnectedScreen.visible = true

func _input(event):
	if event is InputEventMouseButton and %DisconnectedScreen.visible:
		get_tree().change_scene_to_file("res://UI/main_menu.tscn")
