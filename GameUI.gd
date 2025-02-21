extends Node2D
class_name GameUI

const PLAYER_COLORS = ["green", "purple"]
const CENTER_TILE = Vector2i(0, 0)  # tile position of the center of the board
const TOKEN_POSITION = Vector2(0, 0)
const ball_offset = Vector2(0, -4)

@export var board: TileMap
@export var game_manager: GameManager
@export var ball: BallToken
@export var game: AmoeballGame
@export var win_screen_layer: CanvasLayer
@export var game_over_timer: Timer
@export var green_wins_label: Node2D
@export var purple_wins_label: Node2D

var Token = preload("res://blob_token.tscn")
var game_is_over = false

# Coordinate conversion methods
func axial_to_tile(axial: Vector2i) -> Vector2i:
	# Convert axial coordinates to tile coordinates
	# In axial: q = x, r = y
	# For tiles: col = q + (r + (r&1)) / 2, row = r
	var col = axial.x + (axial.y + (axial.y & 1)) / 2
	var row = axial.y
	return Vector2i(col, row)

func tile_to_axial(tile: Vector2i) -> Vector2i:
	# Convert tile coordinates to axial coordinates
	# For tiles: col = q + (r + (r&1)) / 2, row = r
	# Solve for q: q = col - (row + (row&1)) / 2
	var row = tile.y
	var col = tile.x
	var q = col - (row + (row & 1)) / 2
	var r = row
	return Vector2i(q, r)

func world_to_axial(world_pos: Vector2) -> Vector2i:
	# Convert world coordinates to axial coordinates
	var local_pos = to_local(world_pos - board.global_position)
	var tile_pos = board.local_to_map(local_pos)
	return tile_to_axial(tile_pos)

func axial_to_world(axial_pos: Vector2i, is_ball = false) -> Vector2:
	# Convert axial coordinates to world position
	var tile_pos = axial_to_tile(axial_pos)
	return (
	board.map_to_local(tile_pos) +
		Vector2(board.tile_set.tile_size) * TOKEN_POSITION +
		board.global_position +
		(ball_offset if is_ball else Vector2(0, 0))
	)

# Token and game state visualization
func place_token(pos: Vector2i, player: int) -> void:
	var new_token = Token.instantiate()
	add_child(new_token)
	move_child(new_token, 3)

	new_token.position = axial_to_world(pos)
	new_token.animation = PLAYER_COLORS[player] + "_drop"
	new_token.name = "token_%d_%d" % [pos.x, pos.y]
	new_token.flip_h = player
	new_token.show()
	new_token.play()

func remove_token(pos: Vector2i, player: int):
	var token_name = "token_%d_%d" % [pos.x, pos.y]
	var destroyed_node = get_node(token_name)
	destroyed_node.animation = PLAYER_COLORS[player] + "_pop"

func update_ball_pos(new_pos: Vector2i, old_pos: vector2i):
	var anim
	var direction = old_pos - new_pos
	
	# Update visual position based on game state
	ball.position = axial_to_world(old_pos, true)  # Fix drift
	
	# Determine animation based on axial direction
	match direction:
		Vector2i(1, -1): anim = "kick_sw"
		Vector2i(1, 0): anim = "kick_west"
		Vector2i(0, 1): anim = "kick_nw"
		Vector2i(-1, 1): anim = "kick_ne"
		Vector2i(-1, 0): anim = "kick_east"
		Vector2i(0, -1): anim = "kick_se"
	
	ball.play_anim(anim)

func show_win_screen(player: int):
	game_over_timer.start()
	win_screen_layer.visible = true
	if player == 0:
		green_wins_label.visible = true
	else:
		purple_wins_label.visible = true

# Signal handlers for AmoeballGame
func _on_amoeball_game_ball_moved(new_pos: Vector2i, old_pos: Vector2i):
	update_ball_pos(new_pos: Vector2i, old_pos: Vector2i)

func _on_amoeball_game_made_new_token(place: Vector2i, player: int):
	place_token(place, player)

func _on_amoeball_game_removed_token(place: Vector2i, player: int):
	remove_token(place, player)

func _on_amoeball_game_game_over(player: int):
	show_win_screen(player)
	

func _on_amoeball_game_setup():
	game_manager.setup_game()
	
	

func _on_game_over_timer_timeout():
	game_is_over = true

func _input(event):
	if game_is_over and event is InputEventMouseButton:
		get_tree().change_scene_to_file("res://main_menu.tscn")
