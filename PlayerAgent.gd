extends Node2D

@export var player: int
@export var player_color: String

signal make_move(player, move_type, move_cell)
signal check_cursors(player, move_type, move_cell)

enum {ST_WAIT, ST_PLACE, ST_REMOVE, ST_KICK}
var state = ST_WAIT
var valid_moves = []
var ball_pos

const LABEL_TEXT = {
	ST_PLACE:"Player %s, place a token next to an existing token",
	ST_REMOVE:"Player %s, remove one of your tokens",
	ST_KICK:"Player %s, choose a direction to kick the ball"
}
	


var tile_pos
var prev_pos=Vector3(0,0,0)

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
	$PlaceCursor.animation = PLAYER_TOKEN_ANIMS[player]+"_dangle"
	$PlaceCursor.flip_h = player
	$PlaceCursor.play()
	$InstructionLabel.hide()

func _input(event):
	#Need to refactor this
	var cursor
	var event_type
	match state:
		ST_WAIT:
			pass
			
		ST_PLACE:
			cursor = $PlaceCursor
			event_type = "place"
			if event is InputEventMouseMotion:
				tile_pos = world_to_cube(get_global_mouse_position())
				print("moved",get_global_mouse_position(), tile_pos)
				if prev_pos != tile_pos:
					if tile_pos in valid_moves:
						cursor.position = cube_to_world(tile_pos)
						cursor.show()
						cursor.frame=0
						cursor.play()
						if 
					else:
						cursor.hide()
			elif event is InputEventMouseButton and event.pressed:
				tile_pos = world_to_cube(get_global_mouse_position())
				print("clicked",get_global_mouse_position(), tile_pos)
				if tile_pos in valid_moves:
					emit_signal("make_move", player, event_type, tile_pos)

		ST_REMOVE:
			cursor = $RemoveCursor
			event_type="remove"
			
			if event is InputEventMouseMotion:
				tile_pos = world_to_cube(get_global_mouse_position())
				if prev_pos != tile_pos:
					if tile_pos in valid_moves:
						cursor.position = cube_to_world(tile_pos)
						cursor.show()
						cursor.frame=0
						cursor.play()
					else:
						cursor.hide()
					prev_pos = tile_pos
						
			elif event is InputEventMouseButton and event.pressed:
				tile_pos = world_to_cube(get_global_mouse_position())
				print("clicked",get_global_mouse_position(), tile_pos)
				if tile_pos in valid_moves:
					emit_signal("make_move", player, event_type, tile_pos)
					
		ST_KICK:

			cursor = $KickCursor
			event_type = "kick"
			if event is InputEventMouseMotion:
				tile_pos = world_to_cube(get_global_mouse_position())
				if prev_pos != tile_pos:
					if tile_pos in valid_moves:
						cursor.position = cube_to_world(tile_pos)
						cursor.show()
					else:
						cursor.hide()
					prev_pos = tile_pos
						
			elif event is InputEventMouseButton and event.pressed:
				tile_pos = world_to_cube(get_global_mouse_position())
				print("clicked",get_global_mouse_position(), tile_pos)
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
			
