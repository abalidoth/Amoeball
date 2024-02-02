extends Node

var board_size:int = 4
var piece_pos : Array = [[
	Vector3(board_size,0,0),
	Vector3(0,board_size,0),
	Vector3(0,0,board_size)
],[
	Vector3(-board_size,0,0),
	Vector3(0,-board_size,0),
	Vector3(0,0,-board_size)
]]

const HEX_DIRECS = [
	Vector3(1,-1,0),
	Vector3(1,0,-1),
	Vector3(0,1,-1),
	Vector3(0,-1,1),
	Vector3(-1,0,1),
	Vector3(-1,1,0),
	
]

var all_tiles
var current_player = 0
var current_state = STATE_PLACE_1
var ball_pos = Vector3(0,0,0)

var stored_kick_directions

signal made_new_token(place: Vector3)
signal ball_moved(place: Vector3)
signal removed_token(place: Vector3)
signal new_turn(player:int, turn_num:int)
signal made_move(state:Dictionary)

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
			if -board_size <= k <= board_size:
				all_tiles.append(Vector3(i,j,k))
				
	
func is_space_free(t:Vector3):
	return (t not in ([ball_pos]+piece_pos[0]+piece_pos[1]))

func get_free():
	var taken = ([ball_pos]+piece_pos[0]+piece_pos[1])
	var out = [] #gdscript does not have comprehensions
	for i in all_tiles:
		if i not in taken:
			out.append(i)

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
		match hex_dist(ball_pos+i,place):
			1:
				sharp.append(ball_pos+i)
			2:
				curve.append(ball_pos+i)
	if curve != []:
		return curve
	if sharp != []:
		return sharp
	return [] #game is over

func hex_dist(coord_1 : Vector3, coord_2: Vector3):
	return max(abs(coord_1-coord_2))

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
			
func turn_over():
	current_state = STATE_PLACE_1
	current_player = 1-current_player
	current_turn += 1
	new_turn.emit(current_player, current_turn)
	
		
func make_move(move: Vector3):
	state_stack.append({
		"state":current_state,
		"move":move,
		"player":current_player,
		"piece_pos": piece_pos,
		"ball_pos": ball_pos
	})
	made_move.emit(state_stack)
	match current_state:
		STATE_PLACE_1:
			piece_pos[current_player].append(move)
			made_new_token.emit(move)
			stored_kick_directions = get_kick_directions(move)
			match stored_kick_directions:
				null:
					current_state = STATE_REMOVE
				[var a]:
					ball_pos = a
					current_state = STATE_REMOVE
				[var a, var b]:
					current_state = STATE_KICK_1
				[]:
					current_state = STATE_WIN
					
		STATE_KICK_1:
			ball_pos = move
			ball_moved.emit(move)
			current_state = STATE_REMOVE
					
		STATE_REMOVE:
			piece_pos[current_player].erase(move)
			removed_token.emit(move)
			current_state = STATE_PLACE_2
			
		STATE_PLACE_2:
			piece_pos[current_player].append(move)
			made_new_token.emit(move)
			stored_kick_directions = get_kick_directions(move)
			match stored_kick_directions:
				null:
					turn_over()
				[var a]:
					ball_pos = a
					turn_over()
				[var a, var b]:
					current_state = STATE_KICK_2
				[]:
					current_state = STATE_WIN
		
		STATE_KICK_2:
			ball_pos = move
			ball_moved.emit(move)
			turn_over()
