extends Node2D
var old_ball_pos

func _ready():
	old_ball_pos = $BallToken.position
	$GreenBlob2.play()

func play():
	$GreenBlob.animation = "green_drop"
	$BallToken.position = old_ball_pos
	$GreenBlob.play()
	%HowToText.text="""
	When an amoeba is placed
	next to the ball, the ball
	will always be kicked. It
	moves one space, directly
	away from the kicker if
	possible.
	"""
	
	

func _on_green_blob_animation_finished():
	$BallToken.play_anim("kick_nw")
