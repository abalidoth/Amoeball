extends Node

const RADIUS = 4

# Mapping between hex coordinates and array indices
var _coord_to_index: Dictionary = {}
var _adjacency_table: Array[Array] = []
var _index_to_coord: Array[Vector2i] = []

# Fixed array of hex directions for efficient lookup
const DIRECTIONS: Array[Vector2i] = [
	Vector2i(1, 0),    # East  (hex 1)
	Vector2i(1, -1),   # NE    (hex 2)
	Vector2i(0, -1),   # NW    (hex 3)
	Vector2i(-1, 0),   # West  (hex 4)
	Vector2i(-1, 1),   # SW    (hex 5)
	Vector2i(0, 1)     # SE    (hex 6)
]

var total_cells: int = 0

func _ready() -> void:
	# Calculate total cells and build coordinate mappings
	var coords: Array[Vector2i] = []
	
	for q in range(-RADIUS, RADIUS + 1):
		for r in range(-RADIUS, RADIUS + 1):
			if abs(q + r) <= RADIUS:
				var coord = Vector2i(q, r)
				_coord_to_index[coord] = coords.size()
				coords.append(coord)
	
	total_cells = coords.size()
	_index_to_coord = coords
	
	# Precompute adjacency table
	_adjacency_table.resize(total_cells)
	
	for i in range(total_cells):
		var adjacent_indices: Array[int] = []
		var coord = _index_to_coord[i]
		
		for dir in DIRECTIONS:
			var adjacent = coord + dir
			if _coord_to_index.has(adjacent):
				adjacent_indices.append(_coord_to_index[adjacent])
		
		_adjacency_table[i] = adjacent_indices

func is_valid_coordinate(coord: Vector2i) -> bool:
	return _coord_to_index.has(coord)

func get_coord_index(coord: Vector2i) -> int:
	return _coord_to_index.get(coord, -1)

func get_coordinate(index: int) -> Vector2i:
	if index < 0 or index >= total_cells:
		push_error("Index out of range")
		return Vector2i.ZERO
	return _index_to_coord[index]

func get_adjacent_coordinates(coord: Vector2i) -> Array[Vector2i]:
	var index = get_coord_index(coord)
	if index == -1:
		return []
		
	var result: Array[Vector2i] = []
	for adjacent_index in _adjacency_table[index]:
		result.append(_index_to_coord[adjacent_index])
	return result

func get_adjacent_indices(index: int) -> Array[int]:
	if index < 0 or index >= total_cells:
		return []
	return _adjacency_table[index]

func get_distance(a: Vector2i, b: Vector2i) -> int:
	# In axial coordinates, distance is (abs(dq) + abs(dr) + abs(ds))/2
	# where ds = -(dq + dr)
	var diff = a - b
	var dq = abs(diff.x)
	var dr = abs(diff.y)
	var ds = abs(-diff.x - diff.y)
	return (dq + dr + ds) / 2

func transform_coordinate(coord: Vector2i, matrix: Array, translation: Array = [[0, 0], [0, 0]]) -> Vector2i:
	# Apply transformation matrix and translation
	var new_x = matrix[0][0] * coord.x + matrix[0][1] * coord.y + translation[0][0]
	var new_y = matrix[1][0] * coord.x + matrix[1][1] * coord.y + translation[1][0]
	return Vector2i(new_x, new_y)
