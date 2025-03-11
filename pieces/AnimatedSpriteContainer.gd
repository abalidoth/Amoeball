extends CenterContainer

@export var frames:SpriteFrames
@export var anim: String = "default"
@export var sprite_scale: Vector2 = Vector2(1,1)

func _ready():
	%Sprite.sprite_frames = frames
	var heights = []
	var widths = []
	%Sprite.scale = sprite_scale
	
	for i in range(frames.get_frame_count(anim)):
		var t = frames.get_frame_texture(anim,i)
		heights.append(t.get_height())
		widths.append(t.get_width())
	$MarginContainer.custom_minimum_size = Vector2(widths.max(),heights.max())
	
func play():
	%Sprite.play()
	
func set_frame(f):
	%Sprite.frame=f

func stop():
	%Sprite.stop()
