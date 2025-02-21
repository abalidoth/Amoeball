namespace AmoeballAI
{

    public class BoardPermutations
    {
        private static readonly Lazy<BoardPermutations> _instance = new(() => new BoardPermutations());
        public static BoardPermutations Instance => _instance.Value;

        // The 12 elements of the dihedral group D6 as transformation matrices
        private static readonly int[][,] D6Matrices =
        [
            // Pure rotations (6)
            new[,] {{1, 0}, {0, 1}},      // Identity
        new[,] {{0, -1}, {1, 1}},     // Rotate 60° clockwise: (q,r) -> (-r, q+r)
        new[,] {{-1, -1}, {1, 0}},    // Rotate 120° clockwise: (q,r) -> (-q-r, q)
        new[,] {{-1, 0}, {0, -1}},    // Rotate 180°: (q,r) -> (-q, -r)
        new[,] {{0, 1}, {-1, -1}},    // Rotate 240° clockwise: (q,r) -> (r, -q-r)
        new[,] {{1, 1}, {-1, 0}},     // Rotate 300° clockwise: (q,r) -> (q+r, -q)
        
        // Reflections (6) - composed with rotations to preserve coordinate constraints
        new[,] {{-1, -1}, {0, 1}},    // Reflect in primary axis
        new[,] {{-1, 0}, {1, 1}},     // Reflect and rotate 60°
        new[,] {{0, 1}, {1, 0}},      // Reflect and rotate 120°
        new[,] {{1, 1}, {0, -1}},     // Reflect and rotate 180°
        new[,] {{1, 0}, {-1, -1}},    // Reflect and rotate 240°
        new[,] {{0, -1}, {-1, 0}}     // Reflect and rotate 300°
        ];

        // The 12 elements of the dihedral group D6 as index permutations
        private readonly int[][] _permutations;

        // Mapping from board index to its transformed indices under each symmetry
        private readonly int[][] _transformedIndices;

        private BoardPermutations()
        {
            var grid = HexGrid.Instance;
            _permutations = new int[D6Matrices.Length][];
            _transformedIndices = new int[grid.TotalCells][];

            // Pre-compute permutations for each transformation matrix
            for (int t = 0; t < D6Matrices.Length; t++)
            {
                var matrix = D6Matrices[t];
                var permutation = new int[grid.TotalCells];

                // For each position on the board
                for (int i = 0; i < grid.TotalCells; i++)
                {
                    // Get the coordinate, transform it, and get its new index
                    var coord = grid.GetCoordinate(i);
                    var transformed = grid.TransformCoordinate(coord, matrix);
                    var newIndex = grid.GetIndex(transformed);

                    if (newIndex == -1)
                    {
                        throw new InvalidOperationException(
                            $"Invalid transformation: position {coord} transformed to {transformed}");
                    }

                    permutation[i] = newIndex;
                }

                _permutations[t] = permutation;
            }

            // Pre-compute all transformed indices for each board position
            for (int i = 0; i < grid.TotalCells; i++)
            {
                var transformedPositions = new int[D6Matrices.Length];
                for (int t = 0; t < D6Matrices.Length; t++)
                {
                    transformedPositions[t] = _permutations[t][i];
                }
                _transformedIndices[i] = transformedPositions;
            }
        }

        /// <summary>
        /// Applies a permutation to a serialized game state without deserializing
        /// </summary>
        public byte[] ApplyPermutation(byte[] serializedState, int permutationIndex)
        {
            if (permutationIndex < 0 || permutationIndex >= _permutations.Length)
                throw new ArgumentOutOfRangeException(nameof(permutationIndex));

            var permutation = _permutations[permutationIndex];
            var result = new byte[serializedState.Length];

            // Copy header byte (contains current player and turn step)
            result[0] = serializedState[0];

            // Process board array - each cell uses 2 bits
            for (int i = 0; i < HexGrid.Instance.TotalCells; i++)
            {
                int oldByteIndex = 1 + (i >> 2); // Divide by 4 to get byte index
                int oldBitPosition = (i & 3) << 1; // Multiply remainder by 2 to get bit position
                int cellValue = (serializedState[oldByteIndex] >> oldBitPosition) & 0x3;

                int newIndex = permutation[i];
                int newByteIndex = 1 + (newIndex >> 2);
                int newBitPosition = (newIndex & 3) << 1;

                result[newByteIndex] |= (byte)(cellValue << newBitPosition);
            }

            return result;
        }

        /// <summary>
        /// Gets all positions that a given board index transforms to under all symmetries
        /// </summary>
        public ReadOnlySpan<int> GetTransformedIndices(int boardIndex)
        {
            return _transformedIndices[boardIndex];
        }

        /// <summary>
        /// Gets the permutation array for a specific transformation
        /// </summary>
        public ReadOnlySpan<int> GetPermutation(int transformationIndex)
        {
            return _permutations[transformationIndex];
        }

        /// <summary>
        /// Gets the number of transformations (12 for D6 group)
        /// </summary>
        public static int TransformationCount => D6Matrices.Length;

        /// <summary>
        /// Transforms a move from one game state to another, yielding all valid transformations
        /// </summary>
        public IEnumerable<Move> TransformMove(Move move, AmoeballState sourceState, AmoeballState targetState)
        {
            var sourceCache = new SerializedState(sourceState);
            var targetCache = new SerializedState(targetState);
            var grid = HexGrid.Instance;

            // Try each transformation
            for (int i = 0; i < D6Matrices.Length; i++)
            {
                var transformed = new SerializedState(ApplyPermutation(sourceCache.Data.ToArray(), i));
                if (transformed.Equals(targetCache))
                {
                    // Found a matching transformation, apply it to the move
                    var permutation = _permutations[i];
                    var newPosition = permutation[grid.GetIndex(move.Position)];

                    Vector2I? newKickTarget = null;
                    if (move.KickTarget.HasValue)
                    {
                        var kickIndex = grid.GetIndex(move.KickTarget.Value);
                        newKickTarget = grid.GetCoordinate(permutation[kickIndex]);
                    }

                    yield return new Move(grid.GetCoordinate(newPosition), newKickTarget);
                }
            }
        }
    }
}