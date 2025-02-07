using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static AmoeballState;

public readonly struct TransformationCache
{
    private readonly SerializedState _canonicalForm;
    private readonly SerializedState[] _equivalentForms;

    public TransformationCache(AmoeballState state)
    {
        // Get all transformed states
        var transformedStates = Transformations.GetAllTransformedStates(state).ToList();

        // Find canonical form (state with minimum hash)
        _canonicalForm = new SerializedState(transformedStates.MinBy(s => s.GetHashCode())!);

        _equivalentForms = transformedStates.Select(s => new SerializedState(s)).ToArray();
        
    }

    /// <summary>
    /// Checks if a state is equivalent under any transformation to the cached state
    /// </summary>
    public bool Contains(SerializedState state) => _equivalentForms.Contains(state);
    public bool Contains(AmoeballState state) => Contains(new SerializedState(state));


    /// <summary>
    /// Gets the canonical form of the cached state
    /// </summary>
    public AmoeballState CanonicalForm => _canonicalForm.Deserialize();

    public override bool Equals(object? obj)
    {
        return obj is TransformationCache other && Equals(other);
    }

    public bool Equals(TransformationCache other)
    {
       return CanonicalForm.Equals(other.CanonicalForm);
    }

    public override int GetHashCode()
    {
        return _canonicalForm.GetHashCode();
    }

    public static bool operator ==(TransformationCache left, TransformationCache right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TransformationCache left, TransformationCache right)
    {
        return !left.Equals(right);
    }
}

public partial class AmoeballState
{
    public static class Transformations
    {
        // The 12 elements of the dihedral group D6
        // Each matrix represents either a rotation or a reflection-rotation combination
        public static readonly int[][,] D6Matrices = new[]
        {
        // Pure rotations (6)
        new[,] {{1, 0}, {0, 1}},         // Identity
        new[,] {{0, -1}, {1, 1}},        // Rotate 60° clockwise: (q,r) -> (-r, q+r)
        new[,] {{-1, -1}, {1, 0}},       // Rotate 120° clockwise: (q,r) -> (-q-r, q)
        new[,] {{-1, 0}, {0, -1}},       // Rotate 180°: (q,r) -> (-q, -r)
        new[,] {{0, 1}, {-1, -1}},       // Rotate 240° clockwise: (q,r) -> (r, -q-r)
        new[,] {{1, 1}, {-1, 0}},        // Rotate 300° clockwise: (q,r) -> (q+r, -q)
        
        // Reflections (6) - composed with rotations to preserve coordinate constraints
        new[,] {{-1, -1}, {0, 1}},       // Reflect in primary axis
        new[,] {{-1, 0}, {1, 1}},        // Reflect and rotate 60°
        new[,] {{0, 1}, {1, 0}},         // Reflect and rotate 120°
        new[,] {{1, 1}, {0, -1}},        // Reflect and rotate 180°
        new[,] {{1, 0}, {-1, -1}},       // Reflect and rotate 240°
        new[,] {{0, -1}, {-1, 0}}        // Reflect and rotate 300°
    };
        public static AmoeballState TransformState(AmoeballState state, int[,] matrix)
        {
            var newState = new AmoeballState();
            var grid = HexGrid.Instance;

            newState.CurrentPlayer = state.CurrentPlayer;
            newState.TurnStep = state.TurnStep;
            newState.Winner = state.Winner;

            //bool bugReproduced = (matrix[0, 0] == 1 && matrix[0, 1] == -1 && matrix[1, 0] == 1 && matrix[1, 1] == 0);

            for (int i = 0; i < grid.TotalCells; i++)
            {
                var oldPos = grid.GetCoordinate(i);
                var piece = state.GetPiece(oldPos);

                if (piece != PieceType.Empty)
                {
                    var newPos = grid.TransformCoordinate(oldPos, matrix);
                    if (grid.IsValidCoordinate(newPos))
                    {
                        newState.SetPiece(newPos, piece);
                    }
                }
            }

            TransformationValidation.ValidateTransformation(state, newState);

            return newState;
        }

        public static Move TransformMove(Move move, int[,] matrix)
        {
            var grid = HexGrid.Instance;

            var newPosition = grid.TransformCoordinate(move.Position, matrix);

            Vector2I? newKickTarget = null;
            if (move.KickTarget.HasValue)
            {
                newKickTarget = grid.TransformCoordinate(move.KickTarget.Value, matrix);
            }

            return new Move(newPosition, newKickTarget);
        }

        public static IEnumerable<AmoeballState> GetAllTransformedStates(AmoeballState state)
        {
            
            foreach (var sym in D6Matrices)
            {
                var transformed = TransformState(state, sym);
                yield return transformed;

            }
        }

        public static AmoeballState GetCanonicalForm(AmoeballState state)
        {
            return GetAllTransformedStates(state).MinBy(s => s.GetHashCode())!;
        }


        public static IEnumerable<int[,]> GetTransformations(
            AmoeballState sourceState,
            AmoeballState targetState)
        {



            foreach (var sym in D6Matrices)
            {
                var transformed = TransformState(sourceState, sym);
                if (transformed == targetState)
                {
                    yield return sym;
                }
            }
        }

        public static IEnumerable<Move> GetTransformedMoves(
            Move move,
            AmoeballState sourceState,
            AmoeballState targetState)
        {
            foreach (var matrix in GetTransformations(sourceState, targetState))
            {
                yield return TransformMove(move, matrix);
            }
        }
    }
}