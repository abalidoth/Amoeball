extends Control
var ip = ""
func _ready():
	$HTTPRequest.request_completed.connect(_on_request_completed)
	$HTTPRequest.request("https://api.ipify.org")

func _on_request_completed(result, response_code, headers, body):
	ip = body.get_string_from_utf8()
	$CenterContainer/VBoxContainer/Label2.text += ip


func _on_link_button_pressed() -> void:
	DisplayServer.clipboard_set(ip)
