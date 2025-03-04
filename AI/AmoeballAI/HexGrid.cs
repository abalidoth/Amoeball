using Godot;


namespace AmoeballAI
{
    public class HexGrid
    {
        private static readonly Lazy<HexGrid> _instance = new(() => new HexGrid());
        public static HexGrid Instance => _instance.Value;

        public const int RADIUS = 4;
        public readonly int TotalCells;

        // Mapping between hex coordinates and array indices
        private readonly Dictionary<Vector2I, int> _coordToIndex;
        private int[][] _adjacencyTable;
        private readonly Vector2I[] _indexToCoord;

        // Fixed array of hex directions for efficient lookup
        public static readonly Vector2I[] Directions = new Vector2I[]
        {
        new(1, 0),    // East  (hex 1)
        new(1, -1),   // NE    (hex 2)
        new(0, -1),   // NW    (hex 3)
        new(-1, 0),   // West  (hex 4)
        new(-1, 1),   // SW    (hex 5)
        new(0, 1)     // SE    (hex 6)
        };

        private HexGrid()
        {
            // Calculate total cells and build coordinate mappings
            var coords = new List<Vector2I>();
            _coordToIndex = new Dictionary<Vector2I, int>();

            for (int q = -RADIUS; q <= RADIUS; q++)
            {
                for (int r = -RADIUS; r <= RADIUS; r++)
                {
                    if (Math.Abs(q + r) <= RADIUS)
                    {
                        var coord = new Vector2I(q, r);
                        _coordToIndex[coord] = coords.Count;
                        coords.Add(coord);
                    }
                }
            }

            TotalCells = coords.Count;
            _indexToCoord = coords.ToArray();

            // Precompute adjacency table
            _adjacencyTable = new int[TotalCells][];
            for (int i = 0; i < TotalCells; i++)
            {
                var adjacentIndices = new List<int>();
                var coord = _indexToCoord[i];

                foreach (var dir in Directions)
                {
                    var adjacent = coord + dir;
                    if (_coordToIndex.TryGetValue(adjacent, out int adjacentIndex))
                    {
                        adjacentIndices.Add(adjacentIndex);
                    }
                }

                _adjacencyTable[i] = adjacentIndices.ToArray();
            }
        }

        public bool IsValidCoordinate(Vector2I coord)
        {
            return _coordToIndex.ContainsKey(coord);
        }

        public int GetIndex(Vector2I coord)
        {
            return _coordToIndex.TryGetValue(coord, out int index) ? index : -1;
        }

        public Vector2I GetCoordinate(int index)
        {
            if (index < 0 || index >= TotalCells)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _indexToCoord[index];
        }

        public IEnumerable<Vector2I> GetAdjacentCoordinates(Vector2I coord)
        {
            int index = GetIndex(coord);
            if (index != -1)
            {
                foreach (int adjacentIndex in _adjacencyTable[index])
                {
                    yield return _indexToCoord[adjacentIndex];
                }
            }
        }

        public IEnumerable<int> GetAdjacentIndices(int index)
        {
            if (index < 0 || index >= TotalCells)
                return [];
            return _adjacencyTable[index];
        }

        public int GetDistance(Vector2I a, Vector2I b)
        {
            // In axial coordinates, distance is (abs(dq) + abs(dr) + abs(ds))/2
            // where ds = -(dq + dr)
            Vector2I diff = a - b;
            int dq = Math.Abs(diff.X);
            int dr = Math.Abs(diff.Y);
            int ds = Math.Abs(-diff.X - diff.Y);
            return (dq + dr + ds) / 2;
        }
        public Vector2I TransformCoordinate(Vector2I coord, int[,] matrix, int[,]? translation = null)
        {
            translation ??= new int[,] { { 0, 0 }, { 0, 0 } };

            // Apply transformation matrix and translation
            int newX = matrix[0, 0] * coord.X + matrix[0, 1] * coord.Y + translation[0, 0];
            int newY = matrix[1, 0] * coord.X + matrix[1, 1] * coord.Y + translation[1, 0];
            return new Vector2I(newX, newY);
        }
    }
}
