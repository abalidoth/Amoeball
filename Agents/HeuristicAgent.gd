extends AbstractAgent
class_name HeuristicAgent

var move_set: Array

var moves_to_make: Array

func make_move_set(in_game):
	var game :AmoeballGame = in_game.clone()
	var out = []
	var moves_to_eval = [[[],game]]
	while moves_to_eval:
		var move_game = moves_to_eval.pop_back()
		var move_set = move_game[0]
		var new_game = move_game[1]
		var moves = new_game.get_moves()
		if new_game.current_player != player or not moves:
			out.append(move_set)
		else:
			for move in moves:
				var newer_game = new_game.clone()
				newer_game.make_move(move)
				moves_to_eval.append([move_set+[move], newer_game])
	return out
	
	
func evaluate_game_state(game:AmoeballGame):
	return game._state.evaluate_heuristic()

func _handle_game_state_change(new_state, new_player, game):
	if new_state == GameState.STATE_PLACE_1 and new_player == player:
		var max_state = -INF
		var max_moves
		var moves = make_move_set(game)
		var player_mult = -1 if player else 1
		for move_string in moves:
			var new_game = game.clone()
			for move in move_string:
				new_game.make_move(move)
			var evaluation = evaluate_game_state(new_game) * player_mult
			if evaluation > max_state:
				max_state = evaluation
				max_moves = move_string
		moves_to_make = max_moves
				
				
	
func _setup_agent_specific():
	pass


func _on_timer_timeout():
	if moves_to_make:
		var move = moves_to_make.pop_front()
		declare_move.emit(player, "dummy", move)
