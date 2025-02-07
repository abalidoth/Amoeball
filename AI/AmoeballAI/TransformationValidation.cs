using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static AmoeballState;

public static class TransformationValidation
{
    public static void ValidateTransformation(AmoeballState originalState, AmoeballState transformedState)
    {
        // Validate piece count preservation
        ValidatePieceCounts(originalState, transformedState);

        // Validate game state consistency
        ValidateGameState(originalState, transformedState);
    }

    private static void ValidatePieceCounts(AmoeballState original, AmoeballState transformed)
    {
        var grid = HexGrid.Instance;
        var originalCounts = new Dictionary<PieceType, int>();
        var transformedCounts = new Dictionary<PieceType, int>();

        // Count pieces in both states
        for (int i = 0; i < grid.TotalCells; i++)
        {
            var pos = grid.GetCoordinate(i);
            var originalPiece = original.GetPiece(pos);
            var transformedPiece = transformed.GetPiece(pos);

            if (!originalCounts.ContainsKey(originalPiece))
                originalCounts[originalPiece] = 0;
            if (!transformedCounts.ContainsKey(transformedPiece))
                transformedCounts[transformedPiece] = 0;

            originalCounts[originalPiece]++;
            transformedCounts[transformedPiece]++;
        }

        // Compare counts
        foreach (var kvp in originalCounts)
        {
            if (!transformedCounts.ContainsKey(kvp.Key) || transformedCounts[kvp.Key] != kvp.Value)
                throw new InvalidOperationException($"Piece count mismatch for {kvp.Key}: Original={kvp.Value}, Transformed={transformedCounts.GetValueOrDefault(kvp.Key)}");
        }
    }

   

    private static void ValidateGameState(AmoeballState original, AmoeballState transformed)
    {
        // Validate current player
        if (original.CurrentPlayer != transformed.CurrentPlayer)
            throw new InvalidOperationException(
                $"Current player mismatch: Original={original.CurrentPlayer}, " +
                $"Transformed={transformed.CurrentPlayer}");

        // Validate turn step
        if (original.TurnStep != transformed.TurnStep)
            throw new InvalidOperationException(
                $"Turn step mismatch: Original={original.TurnStep}, " +
                $"Transformed={transformed.TurnStep}");

        // Validate winner state
        if (original.Winner != transformed.Winner)
            throw new InvalidOperationException(
                $"Winner mismatch: Original={original.Winner}, " +
                $"Transformed={transformed.Winner}");

        // Validate ball position is transformed correctly
        var originalBallPos = original.GetBallPosition();
        var transformedBallPos = transformed.GetBallPosition();
        if (original.GetPiece(originalBallPos) != transformed.GetPiece(transformedBallPos))
            throw new InvalidOperationException("Ball position not correctly transformed");
    }

    public static void ValidateCanonicalForm(AmoeballState state)
    {
        var canonical = Transformations.GetCanonicalForm(state);

        // Verify canonical form is actually minimal
        foreach (var transformed in Transformations.GetAllTransformedStates(state))
        {
            if (transformed.GetHashCode() < canonical.GetHashCode())
                throw new InvalidOperationException(
                    "Found state with lower hash than supposed canonical form: " +
                    $"Canonical hash={canonical.GetHashCode()}, " +
                    $"Found hash={transformed.GetHashCode()}");
        }
    }

    public static void ValidateMoveTransformation(
        Move originalMove,
        AmoeballState originalState,
        AmoeballState targetState,
        Move transformedMove)
    {
        // Verify the move is valid in both states
        if (!IsValidMove(originalMove, originalState))
            throw new InvalidOperationException("Original move is not valid in original state");

        if (!IsValidMove(transformedMove, targetState))
            throw new InvalidOperationException("Transformed move is not valid in target state");

        // Apply both moves and verify resulting states are equivalent
        var originalResult = ApplyMoveToClone(originalState, originalMove);
        var transformedResult = ApplyMoveToClone(targetState, transformedMove);

        if (Transformations.GetCanonicalForm(originalResult).GetHashCode() !=
            Transformations.GetCanonicalForm(transformedResult).GetHashCode())
            throw new InvalidOperationException("Move transformation produces non-equivalent states");
    }

    private static bool IsValidMove(Move move, AmoeballState state)
    {
        var grid = HexGrid.Instance;

        // Check if position is valid
        if (!grid.IsValidCoordinate(move.Position))
            return false;

        // Check if position is empty
        if (state.GetPiece(move.Position) != PieceType.Empty)
            return false;

        // Check if adjacent to current player's piece
        bool hasAdjacentPiece = false;
        foreach (var adjacent in grid.GetAdjacentCoordinates(move.Position))
        {
            if (state.GetPiece(adjacent) == state.CurrentPlayer)
            {
                hasAdjacentPiece = true;
                break;
            }
        }

        return hasAdjacentPiece;
    }

    private static AmoeballState ApplyMoveToClone(AmoeballState state, Move move)
    {
        var clone = state.Clone();
        clone.ApplyMove(move);
        return clone;
    }
}

public static class TransformationValidator
{
    private const int BOARD_RADIUS = 4;

    public static void ValidateAllTransformationMatrices()
    {
        // Test all D6 matrices against critical board positions
        for (int i = 0; i < Transformations.D6Matrices.Length; i++)
        {
            var matrix = Transformations.D6Matrices[i];
            try
            {
                ValidateTransformationMatrix(matrix, i);
            }
            catch (Exception e)
            {
                throw new Exception($"Matrix {i} failed validation: {e.Message}");
            }
        }

        // Verify we have exactly 12 unique transformations
        ValidateGroupProperties();
    }

    private static void ValidateTransformationMatrix(int[,] matrix, int index)
    {
        // Generate all valid board positions
        var validPositions = GenerateValidBoardPositions();

        foreach (var pos in validPositions)
        {
            // Transform the position
            var newPos = TransformCoordinate(matrix, pos);

            // Validate the transformed position
            if (!IsValidCoordinate(newPos.q, newPos.r))
            {
                throw new Exception(
                    $"Matrix {index} transforms ({pos.q},{pos.r}) to invalid position " +
                    $"({newPos.q},{newPos.r}) with implicit s={-(newPos.q + newPos.r)}");
            }
        }
    }

    private static void ValidateGroupProperties()
    {
        var transformations = Transformations.D6Matrices;
        var seen = new HashSet<string>();

        // Check each transformation produces unique results
        foreach (var matrix in transformations)
        {
            var signature = GetTransformationSignature(matrix);
            if (!seen.Add(signature))
            {
                throw new Exception($"Duplicate transformation found: {signature}");
            }
        }

        if (seen.Count != 12)
        {
            throw new Exception($"Expected 12 unique transformations, found {seen.Count}");
        }
    }

    private static string GetTransformationSignature(int[,] matrix)
    {
        // Transform a few key points and use their results as a signature
        var keyPoints = new[]
        {
            (q: -4, r: 0),
            (q: 4, r: 0),
            (q: 0, r: -4),
            (q: 0, r: 4)
        };

        var transformed = keyPoints.Select(p => TransformCoordinate(matrix, p));
        return string.Join(";", transformed.Select(p => $"{p.q},{p.r}"));
    }

    private static List<(int q, int r)> GenerateValidBoardPositions()
    {
        var positions = new List<(int q, int r)>();

        // Generate all positions within the hexagonal board
        for (int q = -BOARD_RADIUS; q <= BOARD_RADIUS; q++)
        {
            for (int r = -BOARD_RADIUS; r <= BOARD_RADIUS; r++)
            {
                int s = -(q + r);
                if (Math.Abs(s) <= BOARD_RADIUS)
                {
                    positions.Add((q, r));
                }
            }
        }

        return positions;
    }

    private static (int q, int r) TransformCoordinate(int[,] matrix, (int q, int r) pos)
    {
        int newQ = matrix[0, 0] * pos.q + matrix[0, 1] * pos.r;
        int newR = matrix[1, 0] * pos.q + matrix[1, 1] * pos.r;
        return (newQ, newR);
    }

    private static bool IsValidCoordinate(int q, int r)
    {
        int s = -(q + r);

        // Check each coordinate is in range [-4, 4]
        if (Math.Abs(q) > BOARD_RADIUS || Math.Abs(r) > BOARD_RADIUS || Math.Abs(s) > BOARD_RADIUS)
        {
            return false;
        }

        return true;
    }

    public static void ValidateMatrixProperties(int[,] matrix)
    {
        // Check determinant is ±1 (proper rotation or reflection)
        int det = matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];
        if (Math.Abs(det) != 1)
        {
            throw new Exception($"Invalid determinant {det} - must be ±1");
        }

        // Verify matrix preserves hexagon connectivity
        ValidateHexConnectivity(matrix);
    }

    private static void ValidateHexConnectivity(int[,] matrix)
    {
        // Check that adjacent hexes remain adjacent after transformation
        var directions = new[]
        {
            (1, 0), (1, -1), (0, -1),    // First three directions
            (-1, 0), (-1, 1), (0, 1)     // Opposite three directions
        };

        var origin = (q: 0, r: 0);
        var transformedOrigin = TransformCoordinate(matrix, origin);

        foreach (var dir in directions)
        {
            // Get adjacent hex
            var adjacent = (q: origin.q + dir.Item1, r: origin.r + dir.Item2);

            // Transform both points
            var transformedAdjacent = TransformCoordinate(matrix, adjacent);

            // Check they remain adjacent
            int dq = Math.Abs(transformedAdjacent.q - transformedOrigin.q);
            int dr = Math.Abs(transformedAdjacent.r - transformedOrigin.r);
            int ds = Math.Abs((-(transformedAdjacent.q + transformedAdjacent.r)) -
                            (-(transformedOrigin.q + transformedOrigin.r)));

            // In a valid hex grid, adjacent hexes differ by 1 in exactly two coordinates
            int changes = (dq > 0 ? 1 : 0) + (dr > 0 ? 1 : 0) + (ds > 0 ? 1 : 0);
            if (changes != 2 || Math.Max(Math.Max(dq, dr), ds) > 1)
            {
                throw new Exception(
                    $"Matrix does not preserve hex connectivity: " +
                    $"Adjacent hexes ({origin.q},{origin.r}) and ({adjacent.q},{adjacent.r}) " +
                    $"transform to non-adjacent hexes " +
                    $"({transformedOrigin.q},{transformedOrigin.r}) and " +
                    $"({transformedAdjacent.q},{transformedAdjacent.r})");
            }
        }
    }
}