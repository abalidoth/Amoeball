extends Control

var cur_page = 1
var old_page = 1

var max_pages

func _ready():
	max_pages = %ExampleCenter.get_child_count()-1
	page_turn()

func _on_prev_page_pressed():
	if cur_page > 1:
		cur_page -= 1
		page_turn()


func _on_next_page_pressed():
	if cur_page < max_pages:
		cur_page += 1
		page_turn()
		
func page_turn():
	var old_node = get_node("%ExampleCenter/Page"+str(old_page))
	var new_node = get_node("%ExampleCenter/Page"+str(cur_page))
	old_node.visible = false
	new_node.visible = true
	new_node.play()
	%PageNumber.text = str(cur_page)+"/"+str(max_pages)
	%PrevPage.set_enabled(cur_page != 1)
	%NextPage.set_enabled(cur_page != max_pages)
	old_page = cur_page
		


func _on_return_button_pressed():
	get_tree().change_scene_to_file("res://main_menu.tscn")
