extends Node2D
var old_ball_pos

func _ready():
	old_ball_pos = $BallToken.position
	
	$GreenBlob2.play()
	$GreenBlob3.play()
	$PurpleBlob.play()
	$PurpleBlob2.play()
	$PurpleBlob3.play()

func play():
	$GreenBlob.animation = "green_drop"
	$Label.visible=false
	$BallToken.position = old_ball_pos
	$WinCursor.emitting = false
	$GreenBlob.play()
	%HowToText.text="""
	If there is absolutely nowhere
	for the ball to go, you
	immediately win the game!
	"""
	
	

func _on_green_blob_animation_finished():
	$WinCursor.emitting=true
	$Label.visible = true
