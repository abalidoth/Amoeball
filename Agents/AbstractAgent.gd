# abstract_agent.gd
extends Node2D
class_name AbstractAgent

# Enums shared across all agent types
enum GameState {
	STATE_PLACE_1,
	STATE_KICK_1,
	STATE_REMOVE,
	STATE_PLACE_2,
	STATE_KICK_2,
	STATE_WIN,
	STATE_IDLE
}

var player: int = 0  # 0 for green, 1 for purple
var initialized: bool = false
var game_board: AmoeballGame = null
var game_ui: GameUI = null

# Common signals
signal check_cursors(player, move_type, move_cell)

# Common constants
const PURPLE_INDICATOR_POS = Vector2(308, 0)
const PLAYER_TOKEN_ANIMS = ["green", "purple"]
const INDICATOR_DARK = "#777777cc"

# Initialization
func init(agent_name: String, player_id: int, game: AmoeballGame, ui: GameUI):
	if initialized:
		push_error("Agent already initialized")
		return
		
	name = agent_name
	player = player_id
	game_board = game
	game_ui = ui
	game.made_move.connect(_handle_game_state_change)
	game.final_move.connect(_handle_game_state_change)
	
	_setup()
	initialized = true

# Abstract methods that must be implemented by children
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

# Use GameUI's coordinate conversion methods
func world_to_axial(world_pos: Vector2) -> Vector2i:
	return game_ui.world_to_axial(world_pos)

func axial_to_world(axial_pos: Vector2i, is_ball: bool = false) -> Vector2:
	return game_ui.axial_to_world(axial_pos, is_ball)

func _setup():
	if player == 1:  # Purple player setup
		$TurnIndicators.position += PURPLE_INDICATOR_POS
		$TurnIndicators/PlaceIndicator1.animation = "purple_idle"
		$TurnIndicators/PlaceIndicator2.animation = "purple_idle"
		$TurnIndicators/PlaceIndicator1.flip_h = true
		$TurnIndicators/PlaceIndicator2.flip_h = true
