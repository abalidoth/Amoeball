extends Node

var ConnectedRoom: MatchaRoom
var GamePeer: MatchaPeer
var server_active: bool = false

signal server_started(room_id:String)
signal client_has_joined()
signal client_has_left()
signal heard_move(move:String)

func initiate_server() -> void:
	ConnectedRoom = MatchaRoom.create_server_room()
	ConnectedRoom.peer_joined.connect(client_joined)
	ConnectedRoom.peer_left.connect(client_left)
	server_started.emit(ConnectedRoom.room_id)
	server_active = true
	
func connect_to_server(server_id:String):
	ConnectedRoom = MatchaRoom.create_client_room(server_id)
	ConnectedRoom.peer_joined.connect(client_joined)
	ConnectedRoom.peer_left.connect(client_left)
	server_active = true
	
func client_joined(client_id:int, peer:MatchaPeer) -> void:
	GamePeer = peer
	peer.on_event("move",hear_move)
	client_has_joined.emit()
	
func disconnect_server():
	if server_active:
		for peer in ConnectedRoom.get_peers():
			ConnectedRoom.remove_peer(peer)
	
func client_left(client_id:int, peer:MatchaPeer) -> void:
	client_has_left.emit()
	server_active = false

func send_move(move:String) -> void:
	GamePeer.send_event("move",[move])
	
func hear_move(move:String) -> void:
	print(move)
	heard_move.emit(move)
