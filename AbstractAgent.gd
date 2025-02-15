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

@export var player: int = 0  # 0 for green, 1 for purple

# Common signals
signal make_move(player, move_type, move_cell)
signal check_cursors(player, move_type, move_cell)

# Common constants
const PURPLE_INDICATOR_POS = Vector2(308, 0)
const PLAYER_TOKEN_ANIMS = ["green", "purple"]

# Common references
@onready var game_obj = get_node("/root/Game/AmoeballGame")

# Abstract methods that must be implemented by children
func _handle_game_state_change(new_state, new_player, game):
	push_error("Abstract method _handle_game_state_change must be implemented")
	
func _setup_agent_specific():
	push_error("Abstract method _setup_agent_specific must be implemented")

# Common utility methods
func cube_to_world(x: Vector3, ball: bool = false) -> Vector2:
	return $"/root/Game".cube_to_world(x, ball)

func world_to_cube(x: Vector2) -> Vector3:
	return $"/root/Game".world_to_cube(x)

# Common initialization
func _ready():
	_setup_agent_specific()
	_setup_common()

func _setup_common():
	if player == 1:  # Purple player setup
		$TurnIndicators.position += PURPLE_INDICATOR_POS
		$TurnIndicators/PlaceIndicator1.animation = "purple_idle"
		$TurnIndicators/PlaceIndicator2.animation = "purple_idle"
		$TurnIndicators/PlaceIndicator1.flip_h = true
		$TurnIndicators/PlaceIndicator2.flip_h = true
