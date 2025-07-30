class_name AmoeballState
extends RefCounted

enum PieceType {
	EMPTY = 0,
	GREEN_AMOEBA = 1,
	PURPLE_AMOEBA = 2,
	BALL = 3
}

class Move:
	var position: Vector2i
	var kick_target: Vector2i
	
	func _init(pos: Vector2i, kick: Vector2i = AmoeballState._null_position) -> void:
		position = pos
		kick_target = kick
	
	func has_kick() -> bool:
		return kick_target != AmoeballState._null_position

# Efficient fixed-size board representation
var _board: Array[int] = []
var current_player: PieceType = PieceType.EMPTY
var winner: PieceType = PieceType.EMPTY
var turn_step: int = 1
var _ball_position: Vector2i
var last_move: Move

static var _null_position := Vector2i(5, 5)

func _init() -> void:
	_board.resize(HexGrid.total_cells)
	for i in range(_board.size()):
		_board[i] = PieceType.EMPTY

func piece_count() -> int:
	var count = 0
	for piece in _board:
		if piece != PieceType.EMPTY:
			count += 1
	return count

func setup_initial_position() -> void:
	# Clear board
	for i in range(_board.size()):
		_board[i] = PieceType.EMPTY
	
	# Place ball in center
	_ball_position = Vector2i.ZERO
	set_piece(_ball_position, PieceType.BALL)
	
	# Place green amoebas in alternate corners (clockwise from east)
	set_piece(Vector2i(4, 0), PieceType.GREEN_AMOEBA)   # East
	set_piece(Vector2i(-4, 4), PieceType.GREEN_AMOEBA)  # Southwest
	set_piece(Vector2i(0, -4), PieceType.GREEN_AMOEBA)  # Northwest
	
	# Place purple amoebas in remaining corners (clockwise from southeast)
	set_piece(Vector2i(0, 4), PieceType.PURPLE_AMOEBA)  # Southeast
	set_piece(Vector2i(4, -4), PieceType.PURPLE_AMOEBA) # West
	set_piece(Vector2i(-4, 0), PieceType.PURPLE_AMOEBA) # Northeast
	
	current_player = PieceType.GREEN_AMOEBA
	winner = PieceType.EMPTY
	turn_step = 1

func get_piece(pos: Vector2i) -> PieceType:
	var index = HexGrid.get_coord_index(pos)
	return PieceType.EMPTY if index == -1 else _board[index]

func set_piece(pos: Vector2i, piece: PieceType) -> void:
	var index = HexGrid.get_coord_index(pos)
	if index != -1:
		# Update ball position if we're moving the ball
		if _board[index] == PieceType.BALL:
			_ball_position = _null_position
		if piece == PieceType.BALL:
			_ball_position = pos
		
		_board[index] = piece

func is_valid_placement(pos_or_index) -> bool:
	var index: int
	if pos_or_index is Vector2i:
		index = HexGrid.get_coord_index(pos_or_index)
	else:
		index = pos_or_index
	
	if index == -1 or _board[index] != PieceType.EMPTY:
		return false
	
	for adjacent_index in HexGrid.get_adjacent_indices(index):
		if _board[adjacent_index] == current_player:
			return true
	
	return false

func get_legal_moves() -> Array[Move]:
	var moves: Array[Move] = []
	
	if turn_step == 1 or turn_step == 3:
		# Find all valid placements
		for i in range(_board.size()):
			if not is_valid_placement(i):
				continue
			
			var pos = HexGrid.get_coordinate(i)
			
			# Check if this placement would kick the ball
			var will_kick_ball = HexGrid.get_distance(pos, _ball_position) == 1
			
			if will_kick_ball:
				# Generate a move for each possible kick target
				for kick_target in get_kick_destination(pos):
					moves.append(Move.new(pos, kick_target))
			else:
				moves.append(Move.new(pos))
	
	else: # turn_step == 2 (removal)
		# Find all pieces that can be removed
		for i in range(_board.size()):
			if _board[i] == current_player:
				moves.append(Move.new(HexGrid.get_coordinate(i)))
	
	return moves

func get_ball_position() -> Vector2i:
	if _ball_position == _null_position or get_piece(_ball_position) != PieceType.BALL:
		push_error("Ball position is invalid")
		return Vector2i.ZERO
	return _ball_position

func get_kick_destination(kicker_pos: Vector2i) -> Array[Vector2i]:
	var ball_pos = get_ball_position()
	var kicker_dir = kicker_pos - ball_pos
	
	# Find base rotation from kicker direction
	var base_rotation = 0
	for i in range(HexGrid.DIRECTIONS.size()):
		if HexGrid.DIRECTIONS[i] == kicker_dir:
			base_rotation = i
			break
	
	# First priority: Directly opposite (hex 4)
	var target_pos = ball_pos + HexGrid.DIRECTIONS[(base_rotation + 3) % 6]
	if HexGrid.is_valid_coordinate(target_pos) and get_piece(target_pos) == PieceType.EMPTY:
		return [target_pos]
	
	# Second priority: 60° either side (hex 3 or 5)
	var second_priority: Array[Vector2i] = []
	for offset in [2, 4]:
		target_pos = ball_pos + HexGrid.DIRECTIONS[(base_rotation + offset) % 6]
		if HexGrid.is_valid_coordinate(target_pos) and get_piece(target_pos) == PieceType.EMPTY:
			second_priority.append(target_pos)
	if second_priority.size() > 0:
		return second_priority
	
	# Third priority: 120° either side (hex 2 or 6)
	var third_priority: Array[Vector2i] = []
	for offset in [1, 5]:
		target_pos = ball_pos + HexGrid.DIRECTIONS[(base_rotation + offset) % 6]
		if HexGrid.is_valid_coordinate(target_pos) and get_piece(target_pos) == PieceType.EMPTY:
			third_priority.append(target_pos)
	if third_priority.size() > 0:
		return third_priority
	
	# No valid kicks possible - ball is surrounded
	return [ball_pos]

func apply_move(move: Move) -> void:
	match turn_step:
		1, 3: # First placement or Second placement
			apply_placement(move)
		2: # Removal
			apply_removal(move)
		_:
			push_error("Invalid turn step: %d" % turn_step)

func check_for_legal_moves() -> void:
	if get_legal_moves().is_empty():
		winner = PieceType.PURPLE_AMOEBA if current_player == PieceType.GREEN_AMOEBA else PieceType.GREEN_AMOEBA

func apply_removal(move: Move) -> void:
	if get_piece(move.position) != current_player:
		push_error("Can only remove your own pieces")
		return
	
	set_piece(move.position, PieceType.EMPTY)
	last_move = move
	turn_step += 1
	check_for_legal_moves()

func apply_placement(move: Move) -> void:
	# Place the piece
	set_piece(move.position, current_player)
	last_move = move
	
	# If this move includes a kick, move the ball
	if move.has_kick():
		if move.kick_target == _ball_position:
			winner = current_player
			return
		set_piece(_ball_position, PieceType.EMPTY)
		set_piece(move.kick_target, PieceType.BALL)
	
	# Update turn state if this was the second placement
	if turn_step == 3:
		current_player = PieceType.PURPLE_AMOEBA if current_player == PieceType.GREEN_AMOEBA else PieceType.GREEN_AMOEBA
		turn_step = 1
		check_for_legal_moves()
	else:
		turn_step += 1

func clone() -> AmoeballState:
	var new_state = AmoeballState.new()
	new_state._board = _board.duplicate()
	new_state.current_player = current_player
	new_state.turn_step = turn_step
	new_state._ball_position = _ball_position
	new_state.last_move = last_move
	new_state.winner = winner
	return new_state

func is_legal_move(move: Move) -> bool:
	# Check if it's a removal move
	if turn_step == 2:
		return get_piece(move.position) == current_player
	
	# For placement moves (turn_step 1 or 3)
	if not is_valid_placement(move.position):
		return false
	
	# If the placement is adjacent to the ball, validate kick
	if HexGrid.get_distance(move.position, _ball_position) == 1:
		# Get valid kick targets
		var kick_targets = get_kick_destination(move.position)
			
		# If we need to kick but aren't
		if kick_targets.size() > 0 and not move.has_kick():
			return false
			
		# If we're kicking to an invalid target
		if move.has_kick() and not kick_targets.has(move.kick_target):
			return false
	else:
		# If we're not adjacent to the ball but trying to kick
		if move.has_kick():
			return false
	
	return true

func evaluate_heuristic() -> float:
	var out:float = 0.
	if winner == PieceType.GREEN_AMOEBA:
		return INF
	elif winner == PieceType.PURPLE_AMOEBA:
		return -INF
	for i in range(len(_board)):
		var tile = HexGrid.get_coordinate(i)
		var piece = _board[i]
		var dist = HexGrid.get_distance(tile,_ball_position)
		if piece == PieceType.GREEN_AMOEBA:
			out += 1.0/dist
		elif piece == PieceType.PURPLE_AMOEBA:
			out -= 1.0/dist
	return out
