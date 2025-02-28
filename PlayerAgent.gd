extends Node2D

@export var player: int

enum {
	STATE_PLACE_1,
	STATE_KICK_1,
	STATE_REMOVE,
	STATE_PLACE_2,
	STATE_KICK_2,
	STATE_WIN,
	STATE_IDLE
}

signal make_move(player, move_type, move_cell)
signal check_cursors(player, move_type, move_cell)

enum {ST_WAIT, ST_PLACE, ST_REMOVE, ST_KICK}
var state = ST_WAIT
var valid_moves = []
var ball_pos = Vector3(0,0,0)

@onready
var game_obj = get_node("/root/Game/AmoeballGame")

const PURPLE_INDICATOR_POS = Vector2(308,0)

const LABEL_TEXT = {
	ST_PLACE:"Player %s, place a token next to an existing token",
	ST_REMOVE:"Player %s, remove one of your tokens",
	ST_KICK:"Player %s, choose a direction to kick the ball"
}
	

const INDICATOR_DARK = "#777777cc"

var tile_pos
var prev_pos=Vector3(0,0,0)

const PLAYER_TOKEN_ANIMS = ["green","purple"]
# Declare member variables here. Examples:
# var a = 2
# var b = "text"
func cube_to_world(x:Vector3, ball:bool = false) -> Vector2:
	return $"/root/Game".cube_to_world(x, ball)
	

func world_to_cube(x:Vector2) -> Vector3:
	return $"/root/Game".world_to_cube(x)

# Called when the node enters the scene tree for the first time.
func _ready():
	$PlaceCursor.animation = PLAYER_TOKEN_ANIMS[player]+"_dangle"
	$PlaceCursor.flip_h = false
	$PlaceCursor.play()
	$InstructionLabel.hide()
	_on_amoeball_game_made_move(STATE_PLACE_1, 0, null)
		

func set_p2():
	$PlaceCursor.animation = PLAYER_TOKEN_ANIMS[player]+"_dangle"
	$PlaceCursor.flip_h = false
	$TurnIndicators.position += PURPLE_INDICATOR_POS
	$TurnIndicators/PlaceIndicator1.animation = "purple_idle"
	$TurnIndicators/PlaceIndicator2.animation = "purple_idle"
	$TurnIndicators/PlaceIndicator1.flip_h = false
	$TurnIndicators/PlaceIndicator2.flip_h = false
	

func set_nodes_dark():
	for node in $TurnIndicators.get_children():
		node.modulate = INDICATOR_DARK
		node.stop()
	$TurnIndicators/RemoveIndicator.frame = 5

func _input(event):
	#Need to refactor this
	var cursor
	var event_type
	
	if game_obj.current_player != player:
		return
	
	tile_pos = world_to_cube(get_global_mouse_position())
	var valid_moves = game_obj.get_moves()
	
	
	match game_obj.current_state:
		game_obj.STATE_REMOVE:
			if event is InputEventMouseMotion:
				if prev_pos != tile_pos:
					if tile_pos in game_obj.get_moves():
						$RemoveCursor.position = cube_to_world(tile_pos)
						$RemoveCursor.show()
						$RemoveCursor.frame=0
						$RemoveCursor.play()
					else:
						$RemoveCursor.hide()
			elif (
				event is InputEventMouseButton and
				event.pressed and
				event.button_index == MOUSE_BUTTON_LEFT and 
				tile_pos in valid_moves
			):
				game_obj.make_move(tile_pos)
				$RemoveCursor.hide()
				
		game_obj.STATE_PLACE_1, game_obj.STATE_PLACE_2:
			cursor = $PlaceCursor
			if event is InputEventMouseMotion:
				if prev_pos != tile_pos:
					if tile_pos in valid_moves:
						$PlaceCursor.position = cube_to_world(tile_pos)
						$PlaceCursor.show()
						$PlaceCursor.frame=0
						$PlaceCursor.play()
						var moves = game_obj.get_kick_directions(tile_pos)
						match moves:
							null:
								pass
							[var a, var b]:
								$KickCursor.position = cube_to_world(a,true)
								$KickCursor2.position = cube_to_world(b,true)
								$KickCursor.show()
								$KickCursor2.show()
							[var a]:
								$KickCursor.position = cube_to_world(a,true)
								$KickCursor.show()
							[]:
								$WinCursor.position = cube_to_world(game_obj.ball_pos)
								$WinCursor.emitting = true
					else:
						$PlaceCursor.hide()
						$KickCursor2.hide()
						$KickCursor.hide()
						$WinCursor.emitting = false
						
			elif (
				event is InputEventMouseButton and
				event.button_index == MOUSE_BUTTON_LEFT and 
				event.pressed and
				tile_pos in valid_moves
			):
				game_obj.make_move(tile_pos)
				$PlaceCursor.hide()
				$KickCursor2.hide()
				$KickCursor.hide()
				
				
		game_obj.STATE_KICK_1, game_obj.STATE_KICK_2:
			if event is InputEventMouseMotion:
				if prev_pos != tile_pos:
					if tile_pos in game_obj.get_moves():
						$KickCursor.position = cube_to_world(tile_pos, true)
						$KickCursor.show()
					else:
						$KickCursor.hide
			elif (
				event is InputEventMouseButton and
				event.is_released and
				event.button_index == MOUSE_BUTTON_LEFT and 
				tile_pos in valid_moves
			):
				game_obj.make_move(tile_pos)
				$KickCursor.hide()
				
	if event is InputEventMouseMotion:
		prev_pos = tile_pos
		
	
	
			
					

			


func _on_amoeball_game_made_move(new_state, new_player, game):
	set_nodes_dark()
	var light
	if new_player==player:
		match new_state:
			STATE_WIN:
				return
			STATE_PLACE_1:
				light = $TurnIndicators/PlaceIndicator1
			STATE_PLACE_2:
				light = $TurnIndicators/PlaceIndicator2
			STATE_KICK_1:
				light = $TurnIndicators/KickIndicator1
			STATE_KICK_2:
				light = $TurnIndicators/KickIndicator2
			STATE_REMOVE:
				light = $TurnIndicators/RemoveIndicator
		light.modulate = "#ffffff"
		light.frame = 0
		light.play()
	
