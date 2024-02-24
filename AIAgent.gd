extends Node2D

@export var player: int=1
@export var max_plies: int = 5

func _ready():
		$ThinkIndicator1/BallSprite.animation = "stationary"
		$ThinkIndicator1/BallSprite.play()
		$ThinkIndicator.visible= false

func minimax(game, plies):
	var maxing = (game.current_player == 0)
	var best_score
	var best_move
	if maxing:
		best_score = -1000
	else:
		best_score = 1000

	var out_moves = game.get_moves()
	out_moves.sort_custom(func (a,b): return quick_eval_move(game, a) < quick_eval_move(game,b))
	if maxing:
		out_moves.reverse()
	for move in out_moves:
		var subgame = game.duplicate()
		subgame.make_move(move)
		var curr
		if subgame.game_state == subgame.STATE_WIN:
			curr = (100 if subgame.current_player == 0 else -100)
		elif plies >0:
			curr = minimax(subgame, plies-1)[0]
		else:
			curr = full_eval_position(subgame)
			
		if maxing:
			if curr > best_score:
				best_score = curr
				best_move = move
		else:
			if curr < best_score:
				best_score = curr
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
			return player_multiplier/game.hex_dist(game.ball_pos,move)
		game.STATE_REMOVE:
			return -player_multiplier/game.hex_dist(game.ball_pos, move)
		game.STATE_KICK_1, game.STATE_KICK_2:
			return 0
	

func _on_amoeball_game_made_move(new_state, new_player, game):
	if new_player == player:
		$ThinkIndicator.visible = true
		var move = minimax(game, max_plies)
		game.make_move(move)
		$ThinkIndicator.visible = false
