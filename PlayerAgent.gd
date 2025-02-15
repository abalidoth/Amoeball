extends AbstractAgent
class_name PlayerAgent

var valid_moves = []
var ball_pos = Vector3(0,0,0)
var tile_pos
var prev_pos = Vector3(0,0,0)

const INDICATOR_DARK = "#777777cc"

func _setup_agent_specific():
	$PlaceCursor.animation = PLAYER_TOKEN_ANIMS[player] + "_dangle"
	$PlaceCursor.flip_h = player
	$PlaceCursor.play()
	$InstructionLabel.hide()
	_handle_game_state_change(GameState.STATE_PLACE_1, 0, null)

func _handle_game_state_change(new_state, new_player, game):
	set_nodes_dark()
	var light
	if new_player == player:
		match new_state:
			GameState.STATE_WIN:
				return
			GameState.STATE_PLACE_1:
				light = $TurnIndicators/PlaceIndicator1
			GameState.STATE_PLACE_2:
				light = $TurnIndicators/PlaceIndicator2
			GameState.STATE_KICK_1:
				light = $TurnIndicators/KickIndicator1
			GameState.STATE_KICK_2:
				light = $TurnIndicators/KickIndicator2
			GameState.STATE_REMOVE:
				light = $TurnIndicators/RemoveIndicator
		light.modulate = "#ffffff"
		light.frame = 0
		light.play()

func set_nodes_dark():
	for node in $TurnIndicators.get_children():
		node.modulate = INDICATOR_DARK
		node.stop()
	$TurnIndicators/RemoveIndicator.frame = 5

func _input(event):
	if game_obj.current_player != player:
		return
		
	tile_pos = world_to_cube(get_global_mouse_position())
	var valid_moves = game_obj.get_moves()
	
	match game_obj.current_state:
		GameState.STATE_REMOVE:
			_handle_remove_state(event, valid_moves)
		GameState.STATE_PLACE_1, GameState.STATE_PLACE_2:
			_handle_place_state(event, valid_moves)
		GameState.STATE_KICK_1, GameState.STATE_KICK_2:
			_handle_kick_state(event, valid_moves)
	
	if event is InputEventMouseMotion:
		prev_pos = tile_pos

func _handle_remove_state(event, valid_moves):
	if event is InputEventMouseMotion:
		if prev_pos != tile_pos:
			if tile_pos in valid_moves:
				$RemoveCursor.position = cube_to_world(tile_pos)
				$RemoveCursor.show()
				$RemoveCursor.frame = 0
				$RemoveCursor.play()
			else:
				$RemoveCursor.hide()
	elif (event is InputEventMouseButton and
		event.pressed and
		event.button_index == MOUSE_BUTTON_LEFT and 
		tile_pos in valid_moves):
		game_obj.make_move(tile_pos)
		$RemoveCursor.hide()

func _handle_place_state(event, valid_moves):
	if event is InputEventMouseMotion:
		if prev_pos != tile_pos:
			_update_place_cursors(valid_moves)
	elif (event is InputEventMouseButton and
		event.button_index == MOUSE_BUTTON_LEFT and 
		event.pressed and
		tile_pos in valid_moves):
		game_obj.make_move(tile_pos)
		$PlaceCursor.hide()
		$KickCursor2.hide()
		$KickCursor.hide()

func _update_place_cursors(valid_moves):
	if tile_pos in valid_moves:
		$PlaceCursor.position = cube_to_world(tile_pos)
		$PlaceCursor.show()
		$PlaceCursor.frame = 0
		$PlaceCursor.play()
		_update_kick_preview()
	else:
		$PlaceCursor.hide()
		$KickCursor2.hide()
		$KickCursor.hide()
		$WinCursor.emitting = false

func _update_kick_preview():
	var moves = game_obj.get_kick_directions(tile_pos)
	match moves:
		null:
			pass
		[var a, var b]:
			$KickCursor.position = cube_to_world(a, true)
			$KickCursor2.position = cube_to_world(b, true)
			$KickCursor.show()
			$KickCursor2.show()
		[var a]:
			$KickCursor.position = cube_to_world(a, true)
			$KickCursor.show()
		[]:
			$WinCursor.position = cube_to_world(game_obj.ball_pos)
			$WinCursor.emitting = true

func _handle_kick_state(event, valid_moves):
	if event is InputEventMouseMotion:
		if prev_pos != tile_pos:
			if tile_pos in valid_moves:
				$KickCursor.position = cube_to_world(tile_pos, true)
				$KickCursor.show()
			else:
				$KickCursor.hide()
	elif (event is InputEventMouseButton and
		event.is_released and
		event.button_index == MOUSE_BUTTON_LEFT and 
		tile_pos in valid_moves):
		game_obj.make_move(tile_pos)
		$KickCursor.hide()
