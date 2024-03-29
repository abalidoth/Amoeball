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
enum {ST_INIT, ST_PLACE_1, ST_PLACE_2, ST_REMOVE, ST_KICK, ST_END}
var state = ST_INIT

const PLAYER_COLORS=["green","purple"]
const CENTER_TILE = Vector2(0,0) #tile position of the center of the board
const TOKEN_POSITION = Vector2(0,0)

var ALL_TILES = []

var saved_state
var saved_move
var kicked = false

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

func cube_to_world(cube:Vector3) -> Vector2:
	# Convert cubic coordinates to world position (plus shift for token placement)
	return (
		%Board.map_to_local(cube_to_tile(cube))+
		Vector2(%Board.tile_set.tile_size)*TOKEN_POSITION +
		%Board.global_position
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


func place_token(tile:Vector3, player:int) -> void:
	print("placing token", tile, player)
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

# Called when the node enters the scene tree for the first time.
func _ready():
	for player in [0,1]:
		for token in $AmoeballGame.piece_pos[player]:
			place_token(token,player)
	$Ball.position = cube_to_world(Vector3(0,0,0))
	
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
	pass
	
#func advance_game_state(most_recent_move):
	#var this_player = get_player(current_player)
	#var that_player = get_player(1-current_player)
	#match state:
		##Beginning of game
		#ST_INIT:
			#state = ST_PLACE_1
			#this_player.change_state("place")
			#this_player.valid_moves = determine_valid_moves()
		##The player made a move
		#ST_PLACE_1, ST_PLACE_2:
			#if taxicab(ball_pos, most_recent_move)==1 and not kicked: #kick happened!
				#match return_kick_locations(most_recent_move):
					#[var a, var b]: #current player must choose kick
						#saved_state = state
						#saved_move = most_recent_move
						#kicked = true
						#state = ST_KICK
						#this_player.change_state("kick")
						#this_player.valid_moves = [a,b]
						#return #don't advance normally -- we return here after
					#[var a]: #normal kick
						#update_ball_pos(a)
					#[]: #current player wins!
						#state = ST_END
						#this_player.change_state("wait")
						#that_player.change_state("wait")
						#return 
			#if state == ST_PLACE_1:
				#state = ST_REMOVE
				#this_player.change_state("remove")
				#this_player.valid_moves = determine_valid_moves()
			#else: #player's turn is over; switch players
				#state = ST_PLACE_1
				#this_player.change_state("wait")
				#that_player.change_state("place")
				#current_player = 1-current_player
				#that_player.valid_moves = determine_valid_moves()
			#kicked = false
		#
		##The player removed a token
		#ST_REMOVE:
			#state = ST_PLACE_2
			#this_player.change_state("place")
			#this_player.valid_moves = determine_valid_moves()
		#ST_KICK:
			#state = saved_state
			#update_ball_pos(most_recent_move)
			#return advance_game_state(saved_move)
		#ST_END:
			#pass
			
			
						
#	this_player.change_state("wait")
#	that_player.change_state("place")
#	that_player.valid_moves = determine_valid_moves()

func update_ball_pos(cell):
	var anim
	var direction = ball_pos - cell
	
	$Ball.position = cube_to_world(ball_pos) #this is to fix drift
	ball_pos = cell
	print(direction)
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

func determine_valid_moves():
	var moves = []
	match state:
		ST_PLACE_1, ST_PLACE_2: #Select from adjacent to current player's tokens
			print(token_coords[current_player])
			print("checking moves")
			for i in ALL_TILES:
				if tile_free(i):
					for j in token_coords[current_player]:
						if taxicab(i,j) == 1:
							print(i)
							moves.append(i)
							break
		ST_REMOVE:
			return token_coords[current_player]
	return moves
	

func return_kick_locations(move):
	var direct = 2*ball_pos-move
	var far = []
	var near = []
	if tile_free(direct):
		return [direct]
	for t in ALL_TILES:
		if tile_free(t):
			if taxicab(ball_pos, t)==1 and taxicab(move,t)==2:
				#This could include direct, but we already check for that
				far.append(t)
			elif taxicab(ball_pos,t)==1 and taxicab(move,t)==1:
				near.append(t)
	if len(far) != 0:
		return far
	else:
		return near

func remove_token(tile, player):
	print("removing token", tile, player)
	token_coords[player].erase(tile)
	var destroyed_node = get_node("token"+"_"+str(tile.x)+"_"+str(tile.y)+"_"+str(tile.z))
	destroyed_node.animation=PLAYER_COLORS[player]+"_pop" #blob will self delete after
	

#func _on_player_make_move(player, move_type, move_cell):
	#if player == current_player:
		#match move_type:
			#"place":
				#place_token(move_cell, current_player)
			#"remove":
				#remove_token(move_cell, current_player)
		#advance_game_state(move_cell)



func _on_amoeball_game_ball_moved(place):
	update_ball_pos(place)




func _on_amoeball_game_made_new_token(place, player):
	place_token(place, player)


func _on_amoeball_game_new_turn(player, turn_num):
	pass #Don't need this yet


func _on_amoeball_game_removed_token(place, player):
	remove_token(place, player)
