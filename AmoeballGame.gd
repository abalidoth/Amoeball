extends Node
class_name AmoeballGame

signal made_new_token(place: Vector3, player: int)
signal ball_moved(place: Vector3)
signal removed_token(place: Vector3, player: int)
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
	get: return int(_state.CurrentPlayer) - 1
var current_state: int:
	get: return _convert_state_to_enum(_state.TurnStep)
var ball_pos: Vector3:
	get: return _axial_to_cubic(_state.GetBallPosition())
var last_move: Vector3
var current_turn: int = 0

var _state: AmoeballState
var stored_kick_directions
var pending_placement = null

func _init():
	_state = AmoeballState.new()
	_state.SetupInitialPosition()
	last_move = Vector3.ZERO

func _convert_state_to_enum(step: int) -> int:
	match step:
		1: return STATE_PLACE_1
		2: return STATE_REMOVE
		3: return STATE_PLACE_2
	return STATE_IDLE

func _cubic_to_axial(v: Vector3) -> Vector2I:
	return Vector2I(v.x, v.y)

func _axial_to_cubic(v: Vector2I) -> Vector3:
	return Vector3(v.x, v.y, -v.x - v.y)

func is_space_free(t: Vector3) -> bool:
	var pos = _cubic_to_axial(t)
	return _state.GetPiece(pos) == AmoeballState.PieceType.Empty

func get_moves() -> Array:
	if current_state == STATE_KICK_1 or current_state == STATE_KICK_2:
		return stored_kick_directions
	
	var legal_moves = _state.GetLegalMoves()
	var result = []
	for move in legal_moves:
		result.append(_axial_to_cubic(move.Position))
	return result

func get_kick_directions(place: Vector3) -> Array:
	var pos = _cubic_to_axial(place)
	if not _is_adjacent_to_ball(pos):
		return []
		
	var kick_targets = _state.GetKickDestination(pos)
	var result = []
	for target in kick_targets:
		result.append(_axial_to_cubic(target))
	return result

func _is_adjacent_to_ball(pos: Vector2I) -> bool:
	return _state.GetDistance(pos, _state.GetBallPosition()) == 1

func hex_dist(coord_1: Vector3, coord_2: Vector3) -> int:
	var pos1 = _cubic_to_axial(coord_1)
	var pos2 = _cubic_to_axial(coord_2)
	return _state.GetDistance(pos1, pos2)

func set_state(new_state: int) -> void:
	made_move.emit(new_state, current_player, self)

func turn_over() -> void:
	current_player = 1 - current_player
	current_turn += 1
	set_state(STATE_PLACE_1)
	new_turn.emit(current_player, current_turn)

func _handle_placement(move: Vector3, is_second_placement: bool) -> void:
	var pos = _cubic_to_axial(move)
	if not _state.IsValidPlacement(pos):
		push_error("Invalid placement position")
		return
	made_new_token.emit(move, current_player)
	stored_kick_directions = get_kick_directions(move)
	last_move = move
	
	match stored_kick_directions:
		null:
			# No kick needed
			var placement_move = AmoeballState.Move.new(pos)
			if not _state.IsLegalMove(placement_move):
				push_error("Illegal placement move attempted")
				return
			_state.ApplyMove(placement_move)
			if is_second_placement:
				turn_over()
			else:
				set_state(STATE_REMOVE)
		[var a]:
			# Single kick target - automatic
			var kick_move = AmoeballState.Move.new(pos, _cubic_to_axial(a))
			if not _state.IsLegalMove(kick_move):
				push_error("Illegal placement with kick move attempted")
				return
			_state.ApplyMove(kick_move)
			ball_moved.emit(a)
			if is_second_placement:
				turn_over()
			else:
				set_state(STATE_REMOVE)
		[var a, var b]:
			# Multiple kick targets - store placement and wait for kick choice
			pending_placement = move
			set_state(STATE_KICK_2 if is_second_placement else STATE_KICK_1)
		[]:
			# Ball surrounded - game over
			var placement_move = AmoeballState.Move.new(pos)
			if not _state.IsLegalMove(placement_move):
				push_error("Illegal placement move attempted")
				return
			_state.ApplyMove(placement_move)
			set_state(STATE_WIN)
			game_over.emit(current_player)

func _handle_kick(move: Vector3, is_second_kick: bool) -> void:
	var placement_pos = _cubic_to_axial(pending_placement)
	var kick_pos = _cubic_to_axial(move)
	var kick_move = AmoeballState.Move.new(placement_pos, kick_pos)
	
	# Validate the combined placement and kick move
	if not _state.IsLegalMove(kick_move):
		push_error("Illegal kick move attempted")
		return
		
	_state.ApplyMove(kick_move)
	ball_moved.emit(move)
	last_move = pending_placement
	pending_placement = null
	
	if is_second_kick:
		turn_over()
	else:
		set_state(STATE_REMOVE)

func make_move(move: Vector3) -> void:
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
			var pos = _cubic_to_axial(move)
			var remove_move = AmoeballState.Move.new(pos)
			if not _state.IsLegalMove(remove_move):
				push_error("Illegal remove move attempted")
				return
			_state.ApplyMove(remove_move)
			last_move = move
			removed_token.emit(move, current_player)
			set_state(STATE_PLACE_2)
