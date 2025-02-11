#Set this as an Autoload named "HexGrid"

extends Node

const RADIUS = 4

var _coord_to_index: Dictionary = {}
var _index_to_coord: Array[Vector2i] = []
var total_cells: int = 0

# Hex directions in axial coordinates (q,r)
const DIRECTIONS: Array[Vector2i] = [
	Vector2i(1, 0),   # East  (hex 1)
	Vector2i(1, -1),  # NE    (hex 2)
	Vector2i(0, -1),  # NW    (hex 3)
	Vector2i(-1, 0),  # West  (hex 4)
	Vector2i(-1, 1),  # SW    (hex 5)
	Vector2i(0, 1)    # SE    (hex 6)
]

func _init() -> void:
	var coords: Array[Vector2i] = []
	
	# Calculate total cells and build coordinate mappings
	for q in range(-RADIUS, RADIUS + 1):
		for r in range(-RADIUS, RADIUS + 1):
			if abs(q + r) <= RADIUS:
				var coord = Vector2i(q, r)
				_coord_to_index[coord] = coords.size()
				coords.append(coord)
	
	total_cells = coords.size()
	_index_to_coord = coords

func is_valid_coordinate(coord: Vector2i) -> bool:
	return _coord_to_index.has(coord)

func get_index(coord: Vector2i) -> int:
	return _coord_to_index.get(coord, -1)

func get_coordinate(index: int) -> Vector2i:
	assert(index >= 0 and index < total_cells, "Index out of range")
	return _index_to_coord[index]

func get_adjacent_coordinates(coord: Vector2i) -> Array[Vector2i]:
	var result: Array[Vector2i] = []
	for dir in DIRECTIONS:
		var adjacent = coord + dir
		if is_valid_coordinate(adjacent):
			result.append(adjacent)
	return result

func get_adjacent_indices(index: int) -> Array[int]:
	var coord = get_coordinate(index)
	var result: Array[int] = []
	for dir in DIRECTIONS:
		var adjacent = coord + dir
		var adj_index = get_index(adjacent)
		if adj_index != -1:
			result.append(adj_index)
	return result

func get_distance(a: Vector2i, b: Vector2i) -> int:
	# In axial coordinates, distance is (abs(dq) + abs(dr) + abs(ds))/2
	# where ds = -(dq + dr)
	var diff = a - b
	var dq = abs(diff.x)
	var dr = abs(diff.y)
	var ds = abs(-diff.x - diff.y)
	return (dq + dr + ds) / 2

func transform_coordinate(coord: Vector2i, matrix: Array, translation: Array = [[0,0], [0,0]]) -> Vector2i:
	# Apply transformation matrix and translation
	var new_x = matrix[0][0] * coord.x + matrix[0][1] * coord.y + translation[0][0]
	var new_y = matrix[1][0] * coord.x + matrix[1][1] * coord.y + translation[1][0]
	return Vector2i(new_x, new_y)
