extends AbstractAgent
class_name HeuristicAgent

var move_set: Array

var moves_to_make: Array

func make_move_set(in_state):
	var state :AmoeballState = in_state.clone()
	var out = []
	var moves_to_eval = [[[],state]]
	while moves_to_eval:
		var move_state = moves_to_eval.pop_back()
		var move_set = move_state[0]
		var new_state = move_state[1]
		var moves = new_state.get_moves()
		if new_state.current_player != player or not moves:
			out.append(move_set)
		else:
			for move in moves:
				var newer_state = new_state.clone()
				newer_state.make_move(move)
				moves_to_eval.append([move_set+[move], newer_state])
	return out
	
	
func evaluate_game_state(state: AmoeballState) -> float:
	var out:float = 0.
	if state.winner == state.PieceType.GREEN_AMOEBA:
		return INF
	elif state.winner == state.PieceType.PURPLE_AMOEBA:
		return -INF
	for i in range(len(state._board)):
		var tile = HexGrid.get_coordinate(i)
		var piece = state._board[i]
		var dist = HexGrid.get_distance(tile,state._ball_position)
		if piece == state.PieceType.GREEN_AMOEBA:
			out += 1.0/dist
		elif piece == state.PieceType.PURPLE_AMOEBA:
			out -= 1.0/dist
	return out

func _handle_game_state_change(new_state, new_player, game):
	if new_state == GameState.STATE_PLACE_1 and new_player == player:
		var max_state = -INF
		var max_moves
		var moves = make_move_set(new_state)
		var player_mult = -1 if player else 1
		for move_string in moves:
			var test_state = new_state.clone()
			for move in move_string:
				test_state.make_move(move)
			var evaluation = evaluate_game_state(new_state) * player_mult
			if evaluation > max_state:
				max_state = evaluation
				max_moves = move_string
		moves_to_make = max_moves
				
				
	
func _setup_agent_specific():
	pass


func _on_timer_timeout():
	if moves_to_make:
		var move = moves_to_make.pop_front()
		game_board.make_move(move)
