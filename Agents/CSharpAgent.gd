# CsharpAgent.gd
extends AbstractAgent
class_name CsharpAgent

# Reference to the PlayerProvider node that manages the C# Player
@export var player_provider: PlayerProvider

# Buffer for moves for the current turn
var pending_moves: Array = []

# Timer for move delays (optional)
@export var move_delay: float = 0.1

func _setup():
	# Call parent setup first
	super()
	
	if player_provider == null:
		push_error("CsharpAgent: No PlayerProvider assigned!")
		return
	
	# Ensure the provider is in the scene tree
	if not player_provider.is_inside_tree():
		add_child(player_provider)

func _handle_game_state_change(new_state, new_player, game):
	# Call parent handler first
	super(new_state, new_player, game)
	
	if new_player != player:
		return
	
	match new_state:
		GameState.STATE_WIN:
			# Clear any remaining moves on game end
			pending_moves.clear()
			return
			
		GameState.STATE_PLACE_1:
			# Beginning of turn - generate all moves for this turn
			_generate_turn_moves(game)
			_play_next_pending_move()
			
		GameState.STATE_KICK_1, GameState.STATE_REMOVE, GameState.STATE_PLACE_2, GameState.STATE_KICK_2:
			# Just play the next buffered move
			_play_next_pending_move()

func _generate_turn_moves(game):
	if player_provider == null:
		push_error("CsharpAgent: No PlayerProvider available")
		return
	
	# Clear any existing moves
	pending_moves.clear()
	
	# Convert GDScript state to C# state via serialization
	var gdscript_state = game._state
	var serialized = gdscript_state.serialize()
	var csharp_state = AmoeballAI.AmoeballState.new()
	csharp_state.Deserialize(serialized)
	
	# Process the turn (this may run MCTS simulations, etc.)
	player_provider.process_turn(csharp_state)
	
	# Get all three moves for this turn
	var current_state = csharp_state
	for i in range(3):
		var next_state = player_provider.select_single_move(current_state)
		var move = next_state.LastMove
		
		# Add the main move position
		pending_moves.append(move.Position)
		
		# If this is a placement move (steps 1 or 3) and has a kick
		if (i == 0 or i == 2) and move.KickTarget != null:
			pending_moves.append(move.KickTarget)
		
		current_state = next_state
	
	if pending_moves.is_empty():
		push_error("CsharpAgent: No moves generated for turn!")

func _play_next_pending_move():
	if pending_moves.is_empty():
		push_warning("CsharpAgent: No pending moves to play")
		return
	
	var move_position = pending_moves.pop_front()
	
	# Optional delay to make moves visible
	if move_delay > 0:
		await get_tree().create_timer(move_delay).timeout
	
	# Make the move directly on the game board
	game_board.make_move(move_position)

# Get information about pending moves (for debugging)
func get_pending_move_count() -> int:
	return pending_moves.size()

func get_pending_moves_info() -> String:
	var info = "Pending moves: "
	for move in pending_moves:
		info += "%s " % move
	return info

# Utility function to quickly check if agent is ready
func is_ready() -> bool:
	return player_provider != null and player_provider.player_instance != null

# Cleanup
func _exit_tree():
	pending_moves.clear()
