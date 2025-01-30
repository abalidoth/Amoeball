extends Node2D

@export var player: int=1
@export var max_plies: int = 0

signal process_signal

var possible_moves = []
var scores = []
var tries = []
var fitness = []

var base_game

var thinking_flag = false
var moving_flag = false
var moves_to_make = []

var thinks_per_tick = 100

var keep_moves := 0.5
var thinks := 0
var agame = preload("res://amoeball_game.tscn")
func _ready():
		$ThinkIndicator1/BallSprite.animation = "stationary"
		$ThinkIndicator1/BallSprite.play()
		$ThinkIndicator1.visible= false
		
func argmax(l):
	var max_value = -INF
	var arg = 0
	var out_arg = 0
	for i in l:
		if i > max_value:
			out_arg = arg
			max_value = i
		arg += 1
	return out_arg
	
func make_move_set(in_game, prefix = []):
	var game = duplicate_game(in_game)
	var out = []
	if game.current_player != player:
		return [prefix]
	for m in game.get_moves():
		var this_game = duplicate_game(game)
		this_game.make_move(m)
		var subset = make_move_set(this_game, prefix + [m])
		out = out + subset
	return out
		

func monte_carlo_tree_init(game):
	possible_moves = make_move_set(game)
	thinking_flag = true
	moves_to_make = []
	for i in possible_moves:
		scores.append(0)
		tries.append(0)
	$ThinkIndicator1.visible=true
	$Timer.start()
		
	
func _timer_timeout():
	var best_index = argmax_of_uct() #maybe change this to raw wins/tries later
	moving_flag = true
	thinking_flag = false
	moves_to_make = possible_moves[best_index]
	$Timer2.start()
	
func _timer2_timeout():
	if len(moves_to_make)>0 and moving_flag:
		var this_move = moves_to_make.pop_front()
		base_game.make_move(this_move)
	elif moving_flag:
		moving_flag = false
		$Timer2.stop()
		
		$ThinkIndicator1.visible= false
		#any other cleanup that needs done
	
	
func monte_carlo_tree_dive(game, moves):
	var random_flag = false
	var out_moves
	var this_game = duplicate_game(game)
	for m in moves:
		this_game.make_move(m)
	while not this_game.current_state == this_game.STATE_WIN:
		out_moves = this_game.get_moves()
		if len(out_moves) == 0:
			return 1-this_game.current_player
		this_game.make_move(out_moves[randi()%len(out_moves)])
	return this_game.current_player #might need to adjust
	
	

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
	if new_player == player and not thinking_flag:
		base_game = game
		monte_carlo_tree_init(base_game)
		
		
func set_p2():
	pass

func argmax_of_uct():
	var max_value = -INF
	var max_arg = 0
	var total_tries = 0
	for i in tries:
		total_tries += i
	for i in range(len(possible_moves)):
		#below is a lazy way of dealing with div by zero
		var fitness 
		if tries[i] == 0:
			fitness = INF
		else:
			fitness = scores[i]/(tries[i]) + sqrt(2*log(total_tries)/(tries[i]))
		if fitness > max_value:
			max_value = fitness
			max_arg = i
	return max_arg

func _process(delta):
	var selected_index
	if thinking_flag:
		for i in range(thinks_per_tick):
			selected_index = argmax_of_uct()
			var winner = monte_carlo_tree_dive(base_game, possible_moves[selected_index])
			if winner == player:
				scores[selected_index] += 1
			tries[selected_index] += 1
			
			
