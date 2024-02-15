extends Node2D

func play():
	$GreenBlob.play()
	$PurpleBlob.play()
	%HowToText.text = """
	Amoeball is a primordial sport
	played by two teams of amoebas,
	green and purple. Their objective
	is to be the one to completely
	trap the soccer ball.
	"""
