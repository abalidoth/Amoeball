extends Node2D

# Called when the node enters the scene tree for the first time.
func _ready():
	$BallSprite.position = Vector2(0,0)
	$BallSprite.animation = "stationary"


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass

func play_anim(anim):
	$KickAnims.play(anim)

func _on_kick_anims_animation_finished(anim_name):
	self.position+=$BallSprite.position
	$BallSprite.position=Vector2(0,0)
