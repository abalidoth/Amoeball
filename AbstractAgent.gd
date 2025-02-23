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
signal make_move(player, move_type, move_cell)
signal check_cursors(player, move_type, move_cell)

# Common constants
const PURPLE_INDICATOR_POS = Vector2(308, 0)
const PLAYER_TOKEN_ANIMS = ["green", "purple"]

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
	
	_setup_common()
	_setup_agent_specific()
	initialized = true

# Abstract methods that must be implemented by children
func _handle_game_state_change(new_state, new_player, game):
	push_error("Abstract method _handle_game_state_change must be implemented")
	
func _setup_agent_specific():
	push_error("Abstract method _setup_agent_specific must be implemented")

# Use GameUI's coordinate conversion methods
func world_to_axial(world_pos: Vector2) -> Vector2i:
	return game_ui.world_to_axial(world_pos)

func axial_to_world(axial_pos: Vector2i, is_ball: bool = false) -> Vector2:
	return game_ui.axial_to_world(axial_pos, is_ball)

func _setup_common():
	if player == 1:  # Purple player setup
		$TurnIndicators.position += PURPLE_INDICATOR_POS
		$TurnIndicators/PlaceIndicator1.animation = "purple_idle"
		$TurnIndicators/PlaceIndicator2.animation = "purple_idle"
		$TurnIndicators/PlaceIndicator1.flip_h = true
		$TurnIndicators/PlaceIndicator2.flip_h = true
