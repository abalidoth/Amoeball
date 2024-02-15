extends Node2D

func play():
	$Static1.play()
	$Static2.play()
	$Static3.play()
	
	$Green2.visible = true
	$Green2.animation = "green_idle"
	$Green3.visible = false
	$Green3.stop()
	$Green3.animation = "green_drop"
	$Green2.play()
	
	
	%HowToText.text="""
	Your turn consists of three
	actions, taken in order.
	
	1) Place an amoeba.
	2) Remove an amoeba.
	3) Place an amoeba.
	
	All placements must be next
	to one of your existing amoebas.
	"""
	$Green1.animation = "green_drop"
	$Green1.play()
	

func _on_green_1_animation_finished():
	if visible:
		$Remove.visible=true
		$Remove.play()


func _on_remove_animation_finished():
	$Remove.visible = false
	if visible:
		$Green2.animation = "green_pop"
	


func _on_green_2_animation_finished():
	if visible:
		$Green3.visible=true
		$Green3.play()
