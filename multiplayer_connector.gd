extends Node

var ConnectedRoom: MatchaRoom
var GamePeer: MatchaPeer

signal server_started(room_id:String)
signal client_has_joined()
signal client_has_left()
signal heard_move(move:String)

func initiate_server() -> void:
	ConnectedRoom = MatchaRoom.create_server_room()
	ConnectedRoom.peer_joined.connect(client_joined)
	ConnectedRoom.peer_left.connect(client_left)
	server_started.emit(ConnectedRoom.room_id)
	
func connect_to_server(server_id:String):
	ConnectedRoom = MatchaRoom.create_client_room(server_id)
	ConnectedRoom.peer_joined.connect(client_joined)
	ConnectedRoom.peer_left.connect(client_left)
	
func client_joined(client_id:int, peer:MatchaPeer) -> void:
	GamePeer = peer
	peer.on_event("move",hear_move)
	client_has_joined.emit()
	
func client_left(client_id:int, peer:MatchaPeer) -> void:
	client_has_left.emit()

func send_move(move:String) -> void:
	GamePeer.send_event("move",[move])
	
func hear_move(move:String) -> void:
	print(move)
	heard_move.emit(move)
