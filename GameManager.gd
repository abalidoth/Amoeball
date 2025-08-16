extends Node
class_name GameManager

@export var game: AmoeballGame
@export var ui: GameUI

var agents: Array[AbstractAgent] = []

var agent1: AbstractAgent
var agent2: AbstractAgent

func _ready():
	setup_game.call_deferred()

func setup_game():
	# Initialize agents
	agent1 = load(PlayerManager.left_player).instantiate()
	agent2 = load(PlayerManager.right_player).instantiate()
	
	# Add agents to scene
	ui.add_child(agent1)
	ui.add_child(agent2)
	ui.move_child(agent1, 4)
	ui.move_child(agent2, 4)
	
	# Place initial tokens and ball based on game state
	for player in [0, 1]:
		for token in ui.game.get_piece_pos(player):
			ui.place_token(token, player)
	
	ui.ball.position = ui.axial_to_world(game.ball_pos, true)
	
	# Initialize agents
	agent1.init("Player1", 0, game, ui)
	agent2.init("Player2", 1, game, ui)
	
	agents = [agent1, agent2]

func get_agent(index: int) -> AbstractAgent:
	return agents[index]
