extends AnimatedSprite2D


# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass

func handle_animation_stop():
	match self.animation:
		"green_drop":
			self.animation = "green_idle"
			self.play()
		"purple_drop":
			self.animation = "purple_idle"
			self.play()
		"purple_pop", "green_pop":
			self.queue_free()
