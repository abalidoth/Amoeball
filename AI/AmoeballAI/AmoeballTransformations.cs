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
        // Static transformation matrices for 60° rotations in axial coordinates
        public static readonly (int[,], int[,])[] RotationMatrices = new[]
        {
        // Identity (0°)
        (new[,] {{1, 0}, {0, 1}}, new[,] {{0, 0}, {0, 0}}),
        // 60° clockwise
        (new[,] {{1, -1}, {1, 0}}, new[,] {{0, 0}, {0, 0}}),
        // 120° clockwise
        (new[,] {{0, -1}, {1, -1}}, new[,] {{0, 0}, {0, 0}}),
        // 180°
        (new[,] {{-1, 0}, {0, -1}}, new[,] {{0, 0}, {0, 0}}),
        // 120° counterclockwise
        (new[,] {{-1, 1}, {-1, 0}}, new[,] {{0, 0}, {0, 0}}),
        // 60° counterclockwise
        (new[,] {{0, 1}, {-1, 1}}, new[,] {{0, 0}, {0, 0}})
    };

        // Reflection matrices along the 6 symmetry axes
        public static readonly (int[,], int[,])[] ReflectionMatrices = new[]
        {
        // Reflect across vertical axis
        (new[,] {{-1, 0}, {0, 1}}, new[,] {{0, 0}, {0, 0}}),
        // Reflect across 30° axis
        (new[,] {{0, -1}, {-1, 0}}, new[,] {{0, 0}, {0, 0}}),
        // Reflect across 60° axis
        (new[,] {{1, -1}, {0, -1}}, new[,] {{0, 0}, {0, 0}}),
        // Reflect across 90° axis
        (new[,] {{1, 0}, {1, -1}}, new[,] {{0, 0}, {0, 0}}),
        // Reflect across 120° axis
        (new[,] {{0, 1}, {1, 0}}, new[,] {{0, 0}, {0, 0}}),
        // Reflect across 150° axis
        (new[,] {{-1, 1}, {0, 1}}, new[,] {{0, 0}, {0, 0}})
    };

        /// <summary>
        /// Transforms a state using the given transformation matrix and translation
        /// </summary>
        public static AmoeballState TransformState(AmoeballState state, int[,] matrix, int[,] translation)
        {
            var newState = new AmoeballState();
            var grid = HexGrid.Instance;

            newState.CurrentPlayer = state.CurrentPlayer;
            newState.TurnStep = state.TurnStep;
            newState.Winner = state.Winner;

            for (int i = 0; i < grid.TotalCells; i++)
            {
                var oldPos = grid.GetCoordinate(i);
                var piece = state.GetPiece(oldPos);

                if (piece != PieceType.Empty)
                {
                    var newPos = grid.TransformCoordinate(oldPos, matrix, translation);
                    if (grid.IsValidCoordinate(newPos))
                    {
                        newState.SetPiece(newPos, piece);
                    }
                }
            }

            return newState;
        }

        /// <summary>
        /// Transforms a move using the given transformation matrix and translation
        /// </summary>
        public static Move TransformMove(Move move, int[,] matrix, int[,] translation)
        {
            var grid = HexGrid.Instance;

            var newPosition = grid.TransformCoordinate(move.Position, matrix, translation);

            Vector2I? newKickTarget = null;
            if (move.KickTarget.HasValue)
            {
                newKickTarget = grid.TransformCoordinate(move.KickTarget.Value, matrix, translation);
            }

            return new Move(newPosition, newKickTarget);
        }

        /// <summary>
        /// Gets all states that are equivalent under any combination of rotation and reflection
        /// </summary>
        public static IEnumerable<AmoeballState> GetAllTransformedStates(AmoeballState state)
        {
            foreach (var (rotMatrix, rotTranslation) in RotationMatrices)
            {
                var rotatedState = TransformState(state, rotMatrix, rotTranslation);
                yield return rotatedState;

                foreach (var (reflMatrix, reflTranslation) in ReflectionMatrices)
                {
                    yield return TransformState(rotatedState, reflMatrix, reflTranslation);
                }
            }
        }

        /// <summary>
        /// Gets the canonical form of a state (state with minimum hash value)
        /// </summary>
        public static AmoeballState GetCanonicalForm(AmoeballState state)
        {
            return GetAllTransformedStates(state).MinBy(s => s.GetHashCode())!;
        }

        /// <summary>
        /// Composes two transformations into a single transformation
        /// </summary>
        public static (int[,] matrix, int[,] translation) ComposeTransformations(
            int[,] matrix1, int[,] translation1,
            int[,] matrix2, int[,] translation2)
        {
            // Matrix multiplication for the transformation matrices
            var resultMatrix = new int[2, 2];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    resultMatrix[i, j] =
                        matrix1[i, 0] * matrix2[0, j] +
                        matrix1[i, 1] * matrix2[1, j];
                }
            }

            // Compose the translations
            var resultTranslation = new int[2, 2];
            resultTranslation[0, 0] = translation1[0, 0] + translation2[0, 0];
            resultTranslation[1, 0] = translation1[1, 0] + translation2[1, 0];

            return (resultMatrix, resultTranslation);
        }

        /// <summary>
        /// Finds all transformations that convert sourceState into targetState.
        /// Returns empty enumerable if states are not equivalent.
        /// </summary>
        public static IEnumerable<(int[,] matrix, int[,] translation)> GetTransformations(
            AmoeballState sourceState, AmoeballState targetState)
        {
            // Early exit if states aren't equivalent
            if (GetCanonicalForm(sourceState).GetHashCode() != GetCanonicalForm(targetState).GetHashCode())
            {
                yield break;
            }

            foreach (var (rotMatrix, rotTranslation) in RotationMatrices)
            {
                var rotated = TransformState(sourceState, rotMatrix, rotTranslation);
                if (rotated == targetState)
                {
                    yield return (rotMatrix, rotTranslation);
                }

                foreach (var (reflMatrix, reflTranslation) in ReflectionMatrices)
                {
                    var transformed = TransformState(rotated, reflMatrix, reflTranslation);
                    if (transformed == targetState)
                    {
                        yield return ComposeTransformations(
                            rotMatrix, rotTranslation,
                            reflMatrix, reflTranslation);
                    }
                }
            }
        }

        /// <summary>
        /// For each valid transformation between states, yields the resulting transformed move
        /// </summary>
        public static IEnumerable<Move> GetTransformedMoves(Move move, AmoeballState sourceState, AmoeballState targetState)
        {
            foreach (var transform in GetTransformations(sourceState, targetState))
            {
                yield return TransformMove(move, transform.matrix, transform.translation);
            }
        }
    }
}