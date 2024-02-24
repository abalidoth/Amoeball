extends Node

var board_size:int = 4
var piece_pos : Array = [[
	Vector3(-board_size,board_size,0),
	Vector3(0,-board_size,board_size),
	Vector3(board_size,0,-board_size)
],[
	Vector3(board_size,-board_size,0),
	Vector3(0,board_size,-board_size),
	Vector3(-board_size,0,board_size)
]]

const HEX_DIRECS = [
	Vector3(1,-1,0),
	Vector3(1,0,-1),
	Vector3(0,1,-1),
	Vector3(0,-1,1),
	Vector3(-1,0,1),
	Vector3(-1,1,0),
	
]

var all_tiles = []
var current_player = 0
var current_state = STATE_PLACE_1
var ball_pos = Vector3(0,0,0)

var last_move = Vector3(0,0,0)

var stored_kick_directions

signal made_new_token(place: Vector3, player:int)
signal ball_moved(place: Vector3)
signal removed_token(place: Vector3, player:int)
signal new_turn(player:int, turn_num:int)
signal made_move(new_state:int, player:int, game:Node)
signal game_over(player:int)

enum {
	STATE_PLACE_1,
	STATE_KICK_1,
	STATE_REMOVE,
	STATE_PLACE_2,
	STATE_KICK_2,
	STATE_WIN,
	STATE_IDLE
}
var state_stack = []
var current_turn = 0

func _init():
	# get the full set of possible board coords
	for i in range(-board_size,board_size+1):
		for j in range(-board_size,board_size+1):
			var k=-i-j
			if (-board_size <= k) and (k <= board_size):
				all_tiles.append(Vector3(i,j,k))
				
func set_state(new_state):
	made_move.emit(new_state, current_player, self)
	current_state = new_state
				
	
func is_space_free(t:Vector3):
	return (t in all_tiles) and (t not in ([ball_pos]+piece_pos[0]+piece_pos[1]))

func get_free():
	var taken = ([ball_pos]+piece_pos[0]+piece_pos[1])
	var out = [] #gdscript does not have comprehensions
	for i in all_tiles:
		if i not in taken:
			out.append(i)
	return out

func get_kick_directions(place: Vector3):
	if place-ball_pos not in HEX_DIRECS:
		return null
	var curve = []
	var sharp = []
	for i in HEX_DIRECS:
		if not is_space_free(ball_pos+i):
			continue
		if ball_pos-i == place:
			return [ball_pos+i] #straight shot
		var d = hex_dist(ball_pos+i,place)
		if d==1:
			sharp.append(ball_pos+i)
		elif d==2:
			curve.append(ball_pos+i)
	if curve != []:
		return curve
	if sharp != []:
		return sharp
	return [] #game is over

func hex_dist(coord_1 : Vector3, coord_2: Vector3):
	var pos = abs(coord_1-coord_2)
	return max(pos.x, pos.y, pos.z)

func get_moves():
	match current_state:
		STATE_PLACE_1, STATE_PLACE_2:
			var current_tokens = piece_pos[current_player]
			var free_space = get_free()
			var out = []
			for i in free_space:
				for j in current_tokens:
					if hex_dist(i,j)==1:
						out.append(i)
						break
			return(out)
			
		STATE_KICK_1, STATE_KICK_2:
			return stored_kick_directions
		STATE_REMOVE:
			return piece_pos[current_player]
			#var out = []
			#for i in piece_pos[current_player]:
				#if i != last_move:
					#out.append(i)
			#return out
			
func turn_over():
	current_player = 1-current_player
	current_turn += 1
	set_state(STATE_PLACE_1)
	new_turn.emit(current_player, current_turn)
	
	
func state_pop():
	var recent = state_stack.pop_back()
	var new = state_stack[-1]
	current_state = new["state"]
	last_move = new["move"]
	current_player = new["current_player"]
	piece_pos = new["piece_pos"]
	ball_pos = new["ball_pos"]
	return recent
		
func make_move(move: Vector3):
	state_stack.append({
		"state":current_state,
		"move":move,
		"player":current_player,
		"piece_pos": piece_pos,
		"ball_pos": ball_pos
	})
	last_move = move
	match current_state:
		STATE_PLACE_1:
			piece_pos[current_player].append(move)
			made_new_token.emit(move, current_player)
			stored_kick_directions = get_kick_directions(move)
			match stored_kick_directions:
				null:
					set_state(STATE_REMOVE)
					return
				[var a]:
					ball_pos = a
					ball_moved.emit(a)
					set_state(STATE_REMOVE)
					return
				[var a, var b]:
					set_state(STATE_KICK_1)
					return
				[]:
					set_state(STATE_WIN)
					game_over.emit(current_player)
					return
					
		STATE_KICK_1:
			ball_pos = move
			ball_moved.emit(move)
			set_state(STATE_REMOVE)
			return
					
		STATE_REMOVE:
			piece_pos[current_player].erase(move)
			removed_token.emit(move, current_player)
			set_state(STATE_PLACE_2)
			return
			
		STATE_PLACE_2:
			piece_pos[current_player].append(move)
			made_new_token.emit(move, current_player)
			stored_kick_directions = get_kick_directions(move)
			match stored_kick_directions:
				null:
					turn_over()
					return
				[var a]:
					ball_pos = a
					ball_moved.emit(a)
					turn_over()
					return
				[var a, var b]:
					set_state(STATE_KICK_2)
					return
				[]:
					set_state(STATE_WIN)
					game_over.emit(current_player)
					return
		
		STATE_KICK_2:
			ball_pos = move
			ball_moved.emit(move)
			turn_over()
			return
