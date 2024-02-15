extends Button

@export var enabled: bool = true

var fulltext
var hovercolor = "#FFAAFF"
var nohovercolor = "#FFFFFF"
var disabledcolor = "#AAAAAA"


func _ready():
	if not enabled:
		modulate = disabledcolor
	fulltext = text

func set_enabled(x):
	self.enabled = x
	if x:
		modulate = nohovercolor
	else:
		modulate = disabledcolor

func _on_mouse_entered():
	if enabled:
		modulate = hovercolor
		text = "> "+fulltext + " <"


func _on_mouse_exited():
	if enabled:
		modulate = nohovercolor
		text = fulltext
