extends Node2D
var old_ball_pos

func _ready():
	old_ball_pos = $BallToken.position
	$GreenBlob2.play()
	$PurpleBlob.play()
	$PurpleBlob2.play()

func play():
	$GreenBlob.animation = "green_drop"
	$BallToken.position = old_ball_pos
	$GreenBlob.play()
	%HowToText.text="""
	If there's no other option,
	the ball will try to curve
	sharply.
	
	(Again, if there are two
	options, you get to pick.)
	"""
	
	

func _on_green_blob_animation_finished():
	$BallToken.play_anim("kick_sw")
