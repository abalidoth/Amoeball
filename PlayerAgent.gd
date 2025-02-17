extends AbstractAgent
class_name PlayerAgent


var tile_pos: Vector2i
var prev_pos: Vector2i = Vector2i(0, 0)

const INDICATOR_DARK = "#777777cc"

@export var place_cursor: AnimatedSprite2D
@export var remove_cursor: AnimatedSprite2D
@export var kick_cursor: BallToken
@export var kick_cursor2: BallToken
@export var win_cursor: GPUParticles2D
@export var instruction_label: Label

func _setup_agent_specific():
	place_cursor.animation = PLAYER_TOKEN_ANIMS[player] + "_dangle"
	place_cursor.flip_h = player
	place_cursor.play()
	instruction_label.hide()
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
	if game_board.current_player != player:
		return
		
	tile_pos = world_to_axial(get_global_mouse_position())
	var valid_moves = game_board.get_moves()
	
	match game_board.current_state:
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
				remove_cursor.position = axial_to_world(tile_pos)
				remove_cursor.show()
				remove_cursor.frame = 0
				remove_cursor.play()
			else:
				remove_cursor.hide()
	elif (event is InputEventMouseButton and
		event.pressed and
		event.button_index == MOUSE_BUTTON_LEFT and 
		tile_pos in valid_moves):
		game_board.make_move(tile_pos)
		remove_cursor.hide()

func _handle_place_state(event, valid_moves):
	if event is InputEventMouseMotion:
		if prev_pos != tile_pos:
			_update_place_cursors(valid_moves)
	elif (event is InputEventMouseButton and
		event.button_index == MOUSE_BUTTON_LEFT and 
		event.pressed and
		tile_pos in valid_moves):
		game_board.make_move(tile_pos)
		place_cursor.hide()
		kick_cursor2.hide()
		kick_cursor.hide()

func _update_place_cursors(valid_moves):
	if tile_pos in valid_moves:
		place_cursor.position = axial_to_world(tile_pos)
		place_cursor.show()
		place_cursor.frame = 0
		place_cursor.play()
		_update_kick_preview()
	else:
		place_cursor.hide()
		kick_cursor2.hide()
		kick_cursor.hide()
		win_cursor.emitting = false

func _update_kick_preview():
	if !game_board._is_adjacent_to_ball(tile_pos):
		return
	var moves = game_board.get_kick_directions(tile_pos)
	match moves:
		[var a, var b]:
			kick_cursor.position = axial_to_world(a, true)
			kick_cursor2.position = axial_to_world(b, true)
			kick_cursor.show()
			kick_cursor2.show()
		[var a]:
			kick_cursor.position = axial_to_world(a, true)
			kick_cursor.show()
		[]:
			win_cursor.position = axial_to_world(game_board.ball_pos, true)
			win_cursor.emitting = true

func _handle_kick_state(event, valid_moves):
	if event is InputEventMouseMotion:
		if prev_pos != tile_pos:
			if tile_pos in valid_moves:
				kick_cursor.position = axial_to_world(tile_pos, true)
				kick_cursor.show()
			else:
				kick_cursor.hide()
	elif (event is InputEventMouseButton and
		event.is_released and
		event.button_index == MOUSE_BUTTON_LEFT and 
		tile_pos in valid_moves):
		game_board.make_move(tile_pos)
		kick_cursor.hide()
