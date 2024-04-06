extends Node2D

@export var player: int=1
@export var max_plies: int = 0

signal process_signal

var keep_moves := 0.7
var thinks := 0
var agame = preload("res://amoeball_game.tscn")
func _ready():
		$ThinkIndicator1/BallSprite.animation = "stationary"
		$ThinkIndicator1/BallSprite.play()
		$ThinkIndicator1.visible= false

func alpha_beta(game, alpha, beta, plies, parent=false):
	thinks += 1
	if thinks%1000 == 0:
		print(thinks)
	var maxing :bool= (game.current_player == 0)
	var best_score : int
	var best_move
	var move_subgame
	if game.current_state == game.STATE_WIN:
		return [100,null] if game.current_player==0 else [-100,null]
	if plies == 0:
		return [full_eval_position(game),null]
	if maxing:
		best_score = -1000
	else:
		best_score = 1000

	var out_moves = game.get_moves()
	if out_moves == []:
		out_moves = game.get_kick_directions(game.last_move)
	var moves_sort = []
	for j in out_moves:
		moves_sort.append([j,quick_eval_move(game,j)])
	moves_sort.sort_custom(func (a,b): a[1] < b[1])
	out_moves=[]
	for j in moves_sort:
		out_moves.append(j[0])
	if maxing:
		out_moves.reverse()
	for move in out_moves:
		move_subgame = duplicate_game(game)
		move_subgame.make_move(move)
		var curr
		if maxing:
			curr = max(best_move,alpha_beta(move_subgame, -INF, INF,plies-1)[0])
			if curr > beta:
				break
			alpha = max(alpha, curr)
			
		if maxing:
			if curr > best_score:
				best_score = curr
				best_move = move
		else:
			if curr < best_score:
				best_score = curr
				best_move = move
	return [best_score, best_move]


func minimax(game, plies, parent = false):
	thinks += 1
	if thinks%1000 == 0:
		print(thinks)
	var maxing = (game.current_player == 0)
	var best_score
	var best_move = null
	var move_subgame
	if maxing:
		best_score = -1000
	else:
		best_score = 1000

	var out_moves = game.get_moves()
	if out_moves == []:
		out_moves = game.get_kick_directions(game.last_move)
	var moves_sort = []
	for j in out_moves:
		moves_sort.append([j,quick_eval_move(game,j)])
	moves_sort.sort_custom(func (a,b): a[1] < b[1])
	out_moves=[]
	for j in moves_sort:
		out_moves.append(j[0])
	if maxing:
		out_moves.reverse()
		
	if game.current_state in [game.STATE_PLACE_1, game.STATE_PLACE_2]:
		var t: int = ceil(len(out_moves)*keep_moves)+1
		out_moves = out_moves.slice(0,t)
	for move in out_moves:
		move_subgame = duplicate_game(game)
		move_subgame.make_move(move)
		var curr
		if move_subgame.current_state == move_subgame.STATE_WIN:
			curr = (100 if move_subgame.current_player == 0 else -100)
		elif plies >0:
			var curr_l = await minimax(move_subgame, plies-1)
			curr = curr_l[0]
		else:
			curr = full_eval_position(move_subgame)
			
		if maxing:
			if curr > best_score:
				best_score = curr
				if parent:
					best_move = move
		else:
			if curr < best_score:
				best_score = curr
				if parent:
					best_move = move
	return [best_score, best_move]
		
	
func full_eval_position(game):
	var pieces0 = game.piece_pos[0]
	var pieces1 = game.piece_pos[1]
	var out = 0
	for p0 in pieces0:
		out += 1/game.hex_dist(p0, game.ball_pos)
	for p1 in pieces1:
		out -= 1/game.hex_dist(p1, game.ball_pos)
	return out
	
func quick_eval_move(game, move):
	var player_multiplier = (1 if game.current_player==player else -1)
	match game.current_state:
		game.STATE_PLACE_1, game.STATE_PLACE_2:
			return player_multiplier/game.hex_dist(game.ball_pos,move)+randf()*0.01
		game.STATE_REMOVE:
			return -player_multiplier/game.hex_dist(game.ball_pos, move)+randf()*0.01
		game.STATE_KICK_1, game.STATE_KICK_2:
			return 0
	

func duplicate_game(g):
	var subgame = agame.instantiate()
	for i in [
		"current_player",
		"current_state",
		"ball_pos",
		"last_move",
		"current_turn"
	]:
		subgame.set(i,g.get(i))
	subgame.piece_pos[0] = g.piece_pos[0].duplicate()
	subgame.piece_pos[1] = g.piece_pos[1].duplicate()
	if g.stored_kick_directions is Array:
		subgame.stored_kick_directions = g.stored_kick_directions.duplicate()
	return subgame
		

func _on_amoeball_game_made_move(new_state, new_player, game):
	if new_player == player:
		var subgame = duplicate_game(game)
		#for cur_signal in subgame.get_signal_list():
			#var conns = subgame.get_signal_connection_list(cur_signal.name);
			#for cur_conn in conns:
				#cur_conn.signal.disconnect(cur_conn.callable)
		$ThinkIndicator1.visible = true
		var move = await minimax(subgame, max_plies, true)
		game.move_queue.push_front(move[1])
		$ThinkIndicator1.visible = false
		
func set_p2():
	pass
