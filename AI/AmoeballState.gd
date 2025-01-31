extends RefCounted

enum PieceType {
    EMPTY = 0,
    GREEN_AMOEBA = 1,
    PURPLE_AMOEBA = 2,
    BALL = 3
}

var _board: Array = []
var current_player: int = PieceType.GREEN_AMOEBA
var winner: int = PieceType.EMPTY
var turn_step: int = 1
var _ball_position: Vector2i
var last_move: Dictionary  # Stores Move struct data

func _init() -> void:
    # Initialize board array
    _board.resize(HexGrid.total_cells)
    for i in range(_board.size()):
        _board[i] = PieceType.EMPTY

func setup_initial_position() -> void:
    # Clear board
    for i in range(_board.size()):
        _board[i] = PieceType.EMPTY
    
    # Place ball in center
    _ball_position = Vector2i.ZERO
    set_piece(_ball_position, PieceType.BALL)
    
    # Place green amoebas in alternate corners
    set_piece(Vector2i(4, -4), PieceType.GREEN_AMOEBA)
    set_piece(Vector2i(-4, 4), PieceType.GREEN_AMOEBA)
    set_piece(Vector2i(0, -4), PieceType.GREEN_AMOEBA)
    
    # Place purple amoebas in remaining corners
    set_piece(Vector2i(-4, 0), PieceType.PURPLE_AMOEBA)
    set_piece(Vector2i(4, 0), PieceType.PURPLE_AMOEBA)
    set_piece(Vector2i(0, 4), PieceType.PURPLE_AMOEBA)
    
    current_player = PieceType.GREEN_AMOEBA
    winner = PieceType.EMPTY
    turn_step = 1

func get_piece(pos: Vector2i) -> int:
    var index = HexGrid.get_index(pos)
    return _board[index] if index != -1 else PieceType.EMPTY

func set_piece(pos: Vector2i, piece: int) -> void:
    var index = HexGrid.get_index(pos)
    if index != -1:
        # Update ball position if we're moving the ball
        if _board[index] == PieceType.BALL:
            _ball_position = Vector2i.ZERO  # Invalid/no ball position
        if piece == PieceType.BALL:
            _ball_position = pos
        
        _board[index] = piece

func is_valid_placement(pos: Vector2i) -> bool:
    if not HexGrid.is_valid_coordinate(pos) or get_piece(pos) != PieceType.EMPTY:
        return false
    
    # Check if adjacent to current player's amoeba
    for adjacent_pos in HexGrid.get_adjacent_coordinates(pos):
        if get_piece(adjacent_pos) == current_player:
            return true
    
    return false

func get_ball_position() -> Vector2i:
    assert(get_piece(_ball_position) == PieceType.BALL, "Ball position is invalid")
    return _ball_position

func get_kick_destination(kicker_pos: Vector2i) -> Array:
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
    var second_priority = []
    for offset in [2, 4]:
        target_pos = ball_pos + HexGrid.DIRECTIONS[(base_rotation + offset) % 6]
        if HexGrid.is_valid_coordinate(target_pos) and get_piece(target_pos) == PieceType.EMPTY:
            second_priority.append(target_pos)
    if second_priority.size() > 0:
        return second_priority
    
    # Third priority: 120° either side (hex 2 or 6)
    var third_priority = []
    for offset in [1, 5]:
        target_pos = ball_pos + HexGrid.DIRECTIONS[(base_rotation + offset) % 6]
        if HexGrid.is_valid_coordinate(target_pos) and get_piece(target_pos) == PieceType.EMPTY:
            third_priority.append(target_pos)
    if third_priority.size() > 0:
        return third_priority
    
    # No valid kicks possible - ball is surrounded
    return [ball_pos]

func apply_move(move: Dictionary) -> void:
    match turn_step:
        1, 3:  # First placement or Second placement
            apply_placement(move)
        2:  # Removal
            apply_removal(move)
        _:
            push_error("Invalid turn step: %d" % turn_step)

func apply_removal(move: Dictionary) -> void:
    assert(get_piece(move.position) == current_player, "Can only remove your own pieces")
    set_piece(move.position, PieceType.EMPTY)
    last_move = move
    turn_step += 1
    check_for_legal_moves()

func apply_placement(move: Dictionary) -> void:
    # Place the piece
    set_piece(move.position, current_player)
    last_move = move
    
    # If this move includes a kick, move the ball
    if move.has("kick_target"):
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

func check_for_legal_moves() -> void:
    var has_valid_move = false
    for i in range(_board.size()):
        var pos = HexGrid.get_coordinate(i)
        if is_valid_placement(pos):
            has_valid_move = true
            break
    
    if not has_valid_move:
        winner = PieceType.PURPLE_AMOEBA if current_player == PieceType.GREEN_AMOEBA else PieceType.GREEN_AMOEBA
