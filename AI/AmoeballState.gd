extends RefCounted

enum PieceType {
    EMPTY = 0,
    GREEN_AMOEBA = 1,
    PURPLE_AMOEBA = 2,
    BALL = 3
}

# Efficient fixed-size board representation
var _board: Array = []
var current_player: int = PieceType.GREEN_AMOEBA
var turn_step: int = 1
var _ball_position: Vector2i
var last_move = null

class Move:
    var position: Vector2i
    var kick_target: Vector2i  # Will be null if no kick

    func _init(pos: Vector2i, kick: Vector2i = Vector2i.ZERO):
        position = pos
        kick_target = kick

func _init():
    # Initialize empty board
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
    turn_step = 1

func get_piece(pos: Vector2i) -> int:
    var index = HexGrid.get_index(pos)
    return _board[index] if index != -1 else PieceType.EMPTY

func set_piece(pos: Vector2i, piece: int) -> void:
    var index = HexGrid.get_index(pos)
    if index != -1:
        # Update ball position if we're moving the ball
        if _board[index] == PieceType.BALL:
            _ball_position = Vector2i.ZERO
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
    assert(_ball_position != Vector2i.ZERO or get_piece(_ball_position) == PieceType.BALL,
           "Ball position is invalid")
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
    return []

func serialize() -> PackedByteArray:
    # Calculate required size:
    # - 1 byte for current player and turn step (4 bits each)
    # - board array (2 bits per cell, packed into bytes)
    var board_bytes = (_board.size() + 3) / 4  # Ceiling division to pack 2-bit values
    var data = PackedByteArray()
    data.resize(1 + board_bytes)
    
    # Pack current player (2 bits) and turn step (2 bits) into first byte
    data[0] = current_player & 0x3
    data[0] |= (turn_step & 0x3) << 2
    
    # Pack board array - each cell uses 2 bits
    var byte_index = 1
    var bit_position = 0
    var current_byte = 0
    
    for i in range(_board.size()):
        # Pack 2 bits for the current cell
        current_byte |= (_board[i] & 0x3) << bit_position
        bit_position += 2
        
        # When we've packed 4 cells (8 bits), write the byte
        if bit_position == 8:
            data[byte_index] = current_byte
            byte_index += 1
            current_byte = 0
            bit_position = 0
    
    # Write final byte if we have any bits pending
    if bit_position > 0:
        data[byte_index] = current_byte
    
    return data

func deserialize(data: PackedByteArray) -> void:
    assert(data != null and data.size() >= 1, "Invalid serialized data")
    
    # Unpack current player and turn step from first byte
    current_player = data[0] & 0x3
    turn_step = (data[0] >> 2) & 0x3
    
    # Unpack board array and find ball position in single pass
    var byte_index = 1
    var bit_position = 0
    
    for board_index in range(_board.size()):
        assert(byte_index < data.size(), "Serialized data too short")
        
        # Extract 2 bits for current cell
        var cell_value = (data[byte_index] >> bit_position) & 0x3
        _board[board_index] = cell_value
        
        # Update ball position if we found the ball
        if cell_value == PieceType.BALL:
            _ball_position = HexGrid.get_coordinate(board_index)
        
        bit_position += 2
        if bit_position == 8:
            byte_index += 1
            bit_position = 0

static func get_serialized_size(radius: int) -> int:
    # Calculate total cells in hexagonal grid of given radius
    var total_cells = 3 * radius * (radius + 1) + 1
    # Calculate bytes needed: 1 for header + ceil(totalCells * 2 / 8) for board
    return 1 + (total_cells + 3) / 4

func clone() -> AmoeballState:
    var clone = AmoeballState.new()
    clone._board = _board.duplicate()
    clone.current_player = current_player
    clone.turn_step = turn_step
    clone._ball_position = _ball_position
    clone.last_move = last_move
    return clone

func get_next_states() -> Array:
    var states = []
    
    if turn_step == 1 or turn_step == 3:
        # Find all valid placements
        for i in range(_board.size()):
            var pos = HexGrid.get_coordinate(i)
            if is_valid_placement(pos):
                # Check if this placement would kick the ball
                var will_kick_ball = HexGrid.get_distance(pos, _ball_position) == 1
                
                if will_kick_ball:
                    # Generate a state for each possible kick target
                    for kick_target in get_kick_destination(pos):
                        var new_state = clone()
                        new_state.apply_move(Move.new(pos, kick_target))
                        states.append(new_state)
                else:
                    var new_state = clone()
                    new_state.apply_placement(Move.new(pos))
                    states.append(new_state)
    else:  # turn_step == 2 (removal)
        # Find all pieces that can be removed
        for i in range(_board.size()):
            if _board[i] == current_player:
                var pos = HexGrid.get_coordinate(i)
                var new_state = clone()
                new_state.apply_removal(Move.new(pos))
                states.append(new_state)
    
    return states

func apply_move(move: Move) -> void:
    match turn_step:
        1, 3:  # First placement or Second placement
            apply_placement(move)
        2:  # Removal
            apply_removal(move)
        _:
            push_error("Invalid turn step: %d" % turn_step)

func apply_removal(move: Move) -> void:
    assert(get_piece(move.position) == current_player, "Can only remove your own pieces")
    set_piece(move.position, PieceType.EMPTY)
    last_move = move
    turn_step += 1

func apply_placement(move: Move) -> void:
    # Place the piece
    set_piece(move.position, current_player)
    last_move = move
    
    # If this move includes a kick, move the ball
    if move.kick_target != Vector2i.ZERO:
        set_piece(_ball_position, PieceType.EMPTY)
        set_piece(move.kick_target, PieceType.BALL)
    
    # Update turn state if this was the second placement
    if turn_step == 3:
        current_player = PieceType.PURPLE_AMOEBA if current_player == PieceType.GREEN_AMOEBA else PieceType.GREEN_AMOEBA
        turn_step = 1
    else:
        turn_step += 1
