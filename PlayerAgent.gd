extends Node

@export var player: int
@export var player_color: String

signal make_move(player, move_type, move_cell)

enum {ST_WAIT, ST_PLACE, ST_REMOVE, ST_KICK}
var state = ST_WAIT
var valid_moves = []

const LABEL_TEXT = {
	ST_PLACE:"Player %s, place a token next to an existing token",
	ST_REMOVE:"Player %s, remove one of your tokens",
	ST_KICK:"Player %s, choose a direction to kick the ball"
}
	


var tile_pos
var prev_pos

const PLAYER_PARTICLE_COLORS = [Color(0,87, 0.53, 1), Color(0.3, 0.93, 0.4)]
const PLAYER_TOKEN_ANIMS = ["purple","green"]
# Declare member variables here. Examples:
# var a = 2
# var b = "text"
func cube_to_world(x):
	return $"/root/Game".cube_to_world(x)
	

func world_to_cube(x):
	return $"/root/Game".world_to_cube(x)

# Called when the node enters the scene tree for the first time.
func _ready():
	if player == 0:
		$PlaceCursor/purple_particles.emitting = true
	else:
		$PlaceCursor/green_particles.emitting = true
	$PlaceCursor.animation = PLAYER_TOKEN_ANIMS[player]
	$InstructionLabel.hide()

func _input(event):
	var cursor
	var event_type
	match state:
		ST_WAIT:
			pass
		ST_PLACE, ST_REMOVE, ST_KICK:
			if state == ST_PLACE:
				cursor = $PlaceCursor
				event_type="place"
			elif state == ST_REMOVE:
				cursor = $RemoveCursor
				event_type = "remove"
			elif state == ST_KICK:
				cursor = $KickCursor
				event_type = "kick"
			if event is InputEventMouseMotion:
				tile_pos = world_to_cube(event.position)
				if tile_pos in valid_moves:
					cursor.position = cube_to_world(tile_pos)
					cursor.show()
				else:
					cursor.hide()
			elif event is InputEventMouseButton and event.pressed:
				tile_pos = world_to_cube(event.position)
				if tile_pos in valid_moves:
					emit_signal("make_move", player, event_type, tile_pos)
			
					
func change_state(new_state):
	$PlaceCursor.hide()
	$KickCursor.hide()
	$RemoveCursor.hide()
	$InstructionLabel.hide() #Will need text here for AI/multiplayer
	match new_state:
		"wait":
			state = ST_WAIT
		"place":
			state = ST_PLACE
			$InstructionLabel.text = LABEL_TEXT[state] % player
			$InstructionLabel.show()
		"kick":
			state = ST_KICK
			$InstructionLabel.text = LABEL_TEXT[state] % player
			$InstructionLabel.show()
		"remove":
			state = ST_REMOVE
			$InstructionLabel.text = LABEL_TEXT[state] % player
			$InstructionLabel.show()
			
