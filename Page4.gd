extends Node2D
var old_ball_pos

func _ready():
	old_ball_pos = $BallToken.position
	$GreenBlob2.play()
	$PurpleBlob.play()

func play():
	$GreenBlob.animation = "green_drop"
	$BallToken.position = old_ball_pos
	$GreenBlob.play()
	%HowToText.text="""
	If that space is occupied,
	the ball tries to curve
	slightly.
	
	(If there are two options
	for direction, you get to
	pick.)
	"""
	
	

func _on_green_blob_animation_finished():
	$BallToken.play_anim("kick_ne")
