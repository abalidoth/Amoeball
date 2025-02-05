extends Node2D


# Declare member variables here. Examples:
# var a = 2
# var b = "text"
var tile_pos
#var tile_pos_raw

var mouse_pos

var Token = preload("res://blob_token.tscn")
var token_coords = [[],[]]
var ball_pos = Vector3(0,0,0)
var current_player=0 

const PLAYER_COLORS=["green","purple"]
const CENTER_TILE = Vector2(0,0) #tile position of the center of the board
const TOKEN_POSITION = Vector2(0,0)

var ALL_TILES = []

var ball_offset = Vector2(0,-4)

var saved_state
var saved_move
var kicked = false

var game_is_over = false

func tile_to_cube(tile:Vector2) -> Vector3:
	# Convert a tile index into cubic coordinates.
	var tile_c = Vector2(tile)-CENTER_TILE
	var q = tile_c.x - (tile_c.y + (posmod(int(tile_c.y),2)))/2 
	var r = tile_c.y
	return Vector3(q, r, -q-r)
	
func cube_to_tile(cube:Vector3) -> Vector2:
	# Convert cubic coordinates to tile index.
	var col = cube.x + (cube.y + posmod(cube.y, 2)) / 2
	var row = cube.y
	var new_vec = Vector2(col,row) + CENTER_TILE
	return new_vec

func cube_to_world(cube:Vector3, ball=false) -> Vector2:
	# Convert cubic coordinates to world position (plus shift for token placement)
	return (
		%Board.map_to_local(cube_to_tile(cube))+
		Vector2(%Board.tile_set.tile_size)*TOKEN_POSITION +
		%Board.global_position +
		(ball_offset if ball else Vector2(0,0))
	)
	
func world_to_cube(world:Vector2)->Vector3:
	return tile_to_cube(%Board.local_to_map(to_local(world-%Board.global_position)))

func tile_free(tile:Vector3)->bool:
	#Check if a tile is free or occupied by a token
	var taken_pos = (not(tile in token_coords[0]) and
		not (tile in token_coords[1]) and
		not tile==ball_pos
	)
	return %Board.get_cell_source_id(0, cube_to_tile(tile)) != -1 and taken_pos

func taxicab(tile1:Vector3, tile2:Vector3)->int:
	# Hex distance between two tiles
	var dist = tile1-tile2
	return max(abs(dist.x), max(abs(dist.y), abs(dist.z)))

func update_shader(move_pos:Vector3, move_type:String) -> void:
	
	var width : int = 1280
	var height: int = 720
	var uv_factor = Vector2(width,height)/3
	var out=[[],[]]
	var out_ball = cube_to_world(ball_pos)/uv_factor
	for player in range(2):
		for i in range(61):
			if i < len(token_coords[player]):
				out[player].append(cube_to_world(token_coords[player][i])/uv_factor)
			else:
				out[player].append(Vector2(0.0,0.0))
				
	var out_move= cube_to_world(move_pos)/uv_factor
	$BlobShader.update_slimes(
		out,
		[len(token_coords[0]),len(token_coords[1])],
		out_ball,
		out_move,
		move_type,
		$AmoeballGame.current_player
	)

func place_token(tile:Vector3, player:int) -> void:
	var new_token = Token.instantiate()
	add_child(new_token) # to make sure label shows correctly
	move_child(new_token,2)

	new_token.position = cube_to_world(tile)
	token_coords[player].append(tile)
	new_token.animation=PLAYER_COLORS[player]+"_drop"
	new_token.name = "token"+"_"+str(tile.x)+"_"+str(tile.y)+"_"+str(tile.z)
	new_token.flip_h = player
	new_token.show()
	new_token.play()
	update_shader(tile, "place")

var Player1
var Player2

# Called when the node enters the scene tree for the first time.
func _ready():
	Player1 = load(PlayerManager.left_player).instantiate()
	add_child(Player1)
	move_child(Player1,4)
	Player1.name="Player1"
	Player1.player=0
	%AmoeballGame.made_move.connect(Player1._on_amoeball_game_made_move)
	
	Player2 = load(PlayerManager.right_player).instantiate()
	add_child(Player2)
	move_child(Player2,4)
	Player2.name="Player2"
	Player2.player=1
	Player2.set_p2()
	%AmoeballGame.made_move.connect(Player2._on_amoeball_game_made_move)
	
	print(%AmoeballGame.made_move.get_connections())
	
	for player in [0,1]:
		for token in $AmoeballGame.piece_pos[player]:
			place_token(token,player)
	$Ball.position = cube_to_world(Vector3(0,0,0), true)
	
	#make the all_tiles
	for i in range(-4,5):
		for j in range(-4,5):
			for k in range(-4,5):
				if i+j+k == 0:
					ALL_TILES.append(Vector3(i,j,k))
	pass
	
func get_player(i):
	return [$Player1, $Player2][i]

func _input(event):
	if game_is_over:
		if event is InputEventMouseButton:
			get_tree().change_scene_to_file("res://main_menu.tscn")
	


func update_ball_pos(cell):
	var anim
	var direction = ball_pos - cell
	
	$Ball.position = cube_to_world(ball_pos,true) #this is to fix drift
	ball_pos = cell
	match direction:
		Vector3(1,-1,0):
			anim ="kick_sw"
		Vector3(1,0,-1):
			anim ="kick_west"
		Vector3(0,1,-1):
			anim ="kick_nw"
		Vector3(-1,1,0):
			anim ="kick_ne"
		Vector3(-1,0,1):
			anim ="kick_east"
		Vector3(0,-1,1):
			anim ="kick_se"
	$Ball.play_anim(anim)



func remove_token(tile, player):
	token_coords[player].erase(tile)
	var destroyed_node = get_node("token"+"_"+str(tile.x)+"_"+str(tile.y)+"_"+str(tile.z))
	destroyed_node.animation=PLAYER_COLORS[player]+"_pop" #blob will self delete after
	update_shader(tile,"remove")


func show_win_screen(player:int):
	$GameOverTimer.start()
	$WinScreenLayer.visible = true
	if player == 0:
		$WinScreenLayer/ColorRect/Control/GreenWins.visible=true
	else:
		$WinScreenLayer/ColorRect/Control/PurpleWins.visible = true

func _on_amoeball_game_ball_moved(place):
	update_ball_pos(place)


func _on_amoeball_game_made_new_token(place, player):
	place_token(place, player)


func _on_amoeball_game_new_turn(player, turn_num):
	pass #Don't need this yet


func _on_amoeball_game_removed_token(place, player):
	remove_token(place, player)


func _on_amoeball_game_game_over(player):
	show_win_screen(player)


func _on_game_over_timer_timeout():
	game_is_over = true
