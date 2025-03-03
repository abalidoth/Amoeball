extends Node
class_name AmoeballGame

signal made_new_token(place: Vector2i, player: int)
signal ball_moved(new_pos: Vector2i, old_pos: Vector2i)
signal removed_token(place: Vector2i, player: int)
signal new_turn(player: int, turn_num: int)
signal made_move(new_state: int, player: int, game: Node)
signal game_over(player: int)

enum {
	STATE_PLACE_1,
	STATE_KICK_1,
	STATE_REMOVE,
	STATE_PLACE_2,
	STATE_KICK_2,
	STATE_WIN,
	STATE_IDLE
}

var current_player: int:
	get: return int(_state.current_player) - 1
var current_state: int:
	get: return _convert_state_to_enum(_state.turn_step, pending_placement != null)
var ball_pos: Vector2i:
	get: return _state.get_ball_position()
var last_move: Vector2i
var current_turn: int = 0

var _state: AmoeballState
var stored_kick_directions
var pending_placement = null

func _init():
	_state = AmoeballState.new()
	_state.setup_initial_position()
	last_move = Vector2i.ZERO

func _convert_state_to_enum(step: int, kick:bool) -> int:
	if _state.winner != AmoeballState.PieceType.EMPTY:
		return STATE_WIN
	match step:
		1: return STATE_KICK_1 if kick else STATE_PLACE_1
		2: return STATE_REMOVE
		3: return STATE_KICK_2 if kick else STATE_PLACE_2
	return STATE_IDLE
	
func get_piece_pos(player: int) -> Array:
	var positions = []
	var piece_type = AmoeballState.PieceType.GREEN_AMOEBA if player == 0 else AmoeballState.PieceType.PURPLE_AMOEBA
	
	# Iterate through all valid board positions
	for q in range(-4, 5):
		for r in range(-4, 5):
			var pos = Vector2i(q, r)
			# Check if this is a valid board position and contains the player's piece
			if HexGrid.is_valid_coordinate(pos) and _state.get_piece(pos) == piece_type:
				positions.append(pos)
	
	if player == current_player and pending_placement != null:
		positions.append(pending_placement)
	
	return positions

func is_space_free(t: Vector2i) -> bool:
	return _state.get_piece(t) == AmoeballState.PieceType.EMPTY

func get_moves() -> Array:
	if current_state == STATE_KICK_1 or current_state == STATE_KICK_2:
		return stored_kick_directions
	
	var legal_moves = _state.get_legal_moves()
		
	var result = []
	for move in legal_moves:
		result.append(move.position)
	return result

func is_legal_move(move: Vector2i) -> bool:
	var legal_moves = get_moves()
	return legal_moves.has(move)


func get_kick_directions(place: Vector2i) -> Array:
	if not _is_adjacent_to_ball(place):
		return []
		
	var kick_targets = _state.get_kick_destination(place)
	return kick_targets

func _is_adjacent_to_ball(pos: Vector2i) -> bool:
	return HexGrid.get_distance(pos, _state.get_ball_position()) == 1

func hex_dist(coord_1: Vector2i, coord_2: Vector2i) -> int:
	return HexGrid.get_distance(coord_1, coord_2)

func emit_move_signal() -> void:
	if _state.winner != AmoeballState.PieceType.EMPTY:
		game_over.emit(int(_state.winnerr) - 1)
	else:
		made_move.emit(current_state, current_player, self)
	

func turn_over() -> void:
	current_player = 1 - current_player
	current_turn += 1
	emit_move_signal()
	new_turn.emit(current_player, current_turn)

func _handle_placement(move: Vector2i, is_second_placement: bool) -> void:
	var player = current_player
	if not _state.is_valid_placement(move):
		push_error("Invalid placement position")
		return
	stored_kick_directions = get_kick_directions(move)
	last_move = move
	match stored_kick_directions:
		[ball_pos]:
			pass
		[var a]:
			# Single kick target - automatic
			var kick_move = AmoeballState.Move.new(move, a)
			if not _state.is_legal_move(kick_move):
				push_error("Illegal placement with kick move attempted")
				return
			var old_ball_pos = _state.get_ball_position()
			_state.apply_move(kick_move)
			ball_moved.emit(a, old_ball_pos)
			if is_second_placement:
				turn_over()
		[var _a, var _b]:
			# Multiple kick targets - store placement and wait for kick choice
			pending_placement = move
		[]:
			# No Kick OR Ball surrounded - game over
			var placement_move = AmoeballState.Move.new(move)
			if not _state.is_legal_move(placement_move):
				push_error("Illegal placement move attempted")
				return
			_state.apply_move(placement_move)
			if is_second_placement:
				turn_over()
	made_new_token.emit(move, player)

func _handle_kick(move: Vector2i, is_second_kick: bool) -> void:
	var kick_move = AmoeballState.Move.new(pending_placement, move)
	
	# Validate the combined placement and kick move
	if not _state.is_legal_move(kick_move):
		push_error("Illegal kick move attempted")
		return
	
	var old_ball_pos = _state.get_ball_position()
	_state.apply_move(kick_move)
	ball_moved.emit(move, old_ball_pos)
	
	last_move = pending_placement
	pending_placement = null
	
	if is_second_kick:
		turn_over()

func make_move(move: Vector2i) -> void:
	match current_state:
		STATE_PLACE_1:
			_handle_placement(move, false)
		STATE_PLACE_2:
			_handle_placement(move, true)
		STATE_KICK_1:
			_handle_kick(move, false)
		STATE_KICK_2:
			_handle_kick(move, true)
		STATE_REMOVE:
			var remove_move = AmoeballState.Move.new(move)
			if not _state.is_legal_move(remove_move):
				push_error("Illegal remove move attempted")
				return
			_state.apply_move(remove_move)
			last_move = move
			removed_token.emit(move, current_player)
	emit_move_signal()
	
