#Set this as an Autoload named "HexGrid"

extends Node

const RADIUS = 4
var total_cells: int

# Mapping between hex coordinates and array indices
var _coord_to_index: Dictionary = {}
var _index_to_coord: Array = []

# Fixed array of hex directions for efficient lookup
const DIRECTIONS = [
    Vector2i(1, 0),    # East  (hex 1)
    Vector2i(1, -1),   # NE    (hex 2)
    Vector2i(0, -1),   # NW    (hex 3)
    Vector2i(-1, 0),   # West  (hex 4)
    Vector2i(-1, 1),   # SW    (hex 5)
    Vector2i(0, 1)     # SE    (hex 6)
]

func _ready() -> void:
    # Calculate total cells and build coordinate mappings
    var coords = []
    
    for q in range(-RADIUS, RADIUS + 1):
        for r in range(-RADIUS, RADIUS + 1):
            if abs(q) <= RADIUS and abs(r) <= RADIUS and abs(-q - r) <= RADIUS:
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

func get_adjacent_coordinates(coord: Vector2i) -> Array:
    var adjacent = []
    for dir in DIRECTIONS:
        var adj_coord = coord + dir
        if is_valid_coordinate(adj_coord):
            adjacent.append(adj_coord)
    return adjacent

func get_adjacent_indices(index: int) -> Array:
    var coord = get_coordinate(index)
    var adjacent = []
    for dir in DIRECTIONS:
        var adj_coord = coord + dir
        var adj_index = get_index(adj_coord)
        if adj_index != -1:
            adjacent.append(adj_index)
    return adjacent

func get_distance(a: Vector2i, b: Vector2i) -> int:
    # In axial coordinates, distance is (abs(dq) + abs(dr) + abs(ds))/2
    # where ds = -(dq + dr)
    var diff = a - b
    var dq = abs(diff.x)
    var dr = abs(diff.y)
    var ds = abs(-diff.x - diff.y)
    return (dq + dr + ds) / 2
