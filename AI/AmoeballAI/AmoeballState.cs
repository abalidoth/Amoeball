using Godot;
using System;

public partial class AmoeballState
{
    public enum PieceType : byte
    {
        Empty = 0,
        GreenAmoeba = 1,
        PurpleAmoeba = 2,
        Ball = 3
    }

    // Efficient fixed-size board representation
    private readonly PieceType[] _board;
    public PieceType CurrentPlayer { get; private set; }
    public PieceType Winner { get; private set; }
    public int TurnStep { get; private set; }
    private Vector2I _ballPosition;
    public Move LastMove { get; private set; }

    private readonly HexGrid _grid;

    private static readonly Vector2I nullPosition = new Vector2I(5,5);

    public AmoeballState()
    {
        _grid = HexGrid.Instance;
        _board = new PieceType[_grid.TotalCells];
    }

    public void SetupInitialPosition()
    {
        Array.Clear(_board, 0, _board.Length);

        // Place ball in center
        _ballPosition = Vector2I.Zero;
        SetPiece(_ballPosition, PieceType.Ball);

        // Place green amoebas in alternate corners (clockwise from east)
        SetPiece(new Vector2I(4, 0), PieceType.GreenAmoeba);   // East
        SetPiece(new Vector2I(-4, 4), PieceType.GreenAmoeba);  // Southwest
        SetPiece(new Vector2I(0, -4), PieceType.GreenAmoeba); // Northwest

        // Place purple amoebas in remaining corners (clockwise from southeast)
        SetPiece(new Vector2I(0, 4), PieceType.PurpleAmoeba);  // Southeast
        SetPiece(new Vector2I(4, -4), PieceType.PurpleAmoeba); // West
        SetPiece(new Vector2I(-4, 0), PieceType.PurpleAmoeba); // Northeast

        CurrentPlayer = PieceType.GreenAmoeba;
        Winner = PieceType.Empty;
        TurnStep = 1;
    }

    public PieceType GetPiece(Vector2I pos)
    {
        int index = _grid.GetIndex(pos);
        return index != -1 ? _board[index] : PieceType.Empty;
    }

    private void SetPiece(Vector2I pos, PieceType piece)
    {
        int index = _grid.GetIndex(pos);
        if (index != -1)
        {
            // Update ball position if we're moving the ball
            if (_board[index] == PieceType.Ball)
            {
                _ballPosition = Vector2I.Zero; // Invalid/no ball position
            }
            if (piece == PieceType.Ball)
            {
                _ballPosition = pos;
            }

            _board[index] = piece;
        }
    }

    public bool IsValidPlacement(Vector2I pos)
    {
        if (!_grid.IsValidCoordinate(pos) || GetPiece(pos) != PieceType.Empty)
            return false;

        // Check if adjacent to current player's amoeba
        foreach (var adjacentPos in _grid.GetAdjacentCoordinates(pos))
        {
            if (GetPiece(adjacentPos) == CurrentPlayer)
                return true;
        }

        return false;
    }

    public Vector2I GetBallPosition()
    {
        if (_ballPosition == nullPosition || GetPiece(_ballPosition) != PieceType.Ball)
        {
            throw new InvalidOperationException("Ball position is invalid");
        }
        return _ballPosition;
    }

    public Vector2I[] GetKickDestination(Vector2I kickerPos)
    {
        var ballPos = GetBallPosition();
        var kickerDir = kickerPos - ballPos;

        // Find base rotation from kicker direction
        int baseRotation = 0;
        for (int i = 0; i < HexGrid.Directions.Length; i++)
        {
            if (HexGrid.Directions[i] == kickerDir)
            {
                baseRotation = i;
                break;
            }
        }

        // First priority: Directly opposite (hex 4)
        var targetPos = ballPos + HexGrid.Directions[(baseRotation + 3) % 6];
        if (_grid.IsValidCoordinate(targetPos) && GetPiece(targetPos) == PieceType.Empty)
        {
            return new[] { targetPos };
        }

        // Second priority: 60° either side (hex 3 or 5)
        var secondPriority = new List<Vector2I>();
        foreach (int offset in new[] { 2, 4 })
        {
            targetPos = ballPos + HexGrid.Directions[(baseRotation + offset) % 6];
            if (_grid.IsValidCoordinate(targetPos) && GetPiece(targetPos) == PieceType.Empty)
            {
                secondPriority.Add(targetPos);
            }
        }
        if (secondPriority.Count > 0)
        {
            return secondPriority.ToArray();
        }

        // Third priority: 120° either side (hex 2 or 6)
        var thirdPriority = new List<Vector2I>();
        foreach (int offset in new[] { 1, 5 })
        {
            targetPos = ballPos + HexGrid.Directions[(baseRotation + offset) % 6];
            if (_grid.IsValidCoordinate(targetPos) && GetPiece(targetPos) == PieceType.Empty)
            {
                thirdPriority.Add(targetPos);
            }
        }
        if (thirdPriority.Count > 0)
        {
            return thirdPriority.ToArray();
        }

        // No valid kicks possible - ball is surrounded
        return new[] { ballPos };
    }

    // Serialization to byte array for efficient network transmission
    public byte[] Serialize()
    {
        // Calculate required size:
        // - 1 byte for current player and turn step (4 bits each)
        // - board array (2 bits per cell, packed into bytes)
        int boardBytes = (_board.Length + 3) / 4; // Ceiling division to pack 2-bit values
        byte[] data = new byte[1 + boardBytes];

        // Pack current player (2 bits) and turn step (2 bits) into first byte
        data[0] = (byte)((byte)CurrentPlayer & 0x3);
        data[0] |= (byte)((TurnStep & 0x3) << 2);

        // Pack board array - each cell uses 2 bits
        int byteIndex = 1;
        int bitPosition = 0;
        byte currentByte = 0;

        for (int i = 0; i < _board.Length; i++)
        {
            // Pack 2 bits for the current cell
            currentByte |= (byte)(((byte)_board[i] & 0x3) << bitPosition);
            bitPosition += 2;

            // When we've packed 4 cells (8 bits), write the byte
            if (bitPosition == 8)
            {
                data[byteIndex++] = currentByte;
                currentByte = 0;
                bitPosition = 0;
            }
        }

        // Write final byte if we have any bits pending
        if (bitPosition > 0)
        {
            data[byteIndex] = currentByte;
        }

        return data;
    }

    public void Deserialize(byte[] data)
    {
        if (data == null || data.Length < 1)
            throw new ArgumentException("Invalid serialized data");

        // Unpack current player and turn step from first byte
        CurrentPlayer = (PieceType)(data[0] & 0x3);
        TurnStep = (data[0] >> 2) & 0x3;

        if (CurrentPlayer == PieceType.Empty || CurrentPlayer == PieceType.Ball)
            throw new ArgumentException("Invalid current player");

        // Unpack board array and find ball position in single pass
        int byteIndex = 1;
        int bitPosition = 0;

        _ballPosition = nullPosition;

        for (int boardIndex = 0; boardIndex < _board.Length; boardIndex++)
        {
            if (byteIndex >= data.Length)
                throw new ArgumentException("Serialized data too short");

            // Extract 2 bits for current cell
            byte cellValue = (byte)((data[byteIndex] >> bitPosition) & 0x3);
            _board[boardIndex] = (PieceType)cellValue;

            // Update ball position if we found the ball
            if (cellValue == (byte)PieceType.Ball)
            {
                _ballPosition = _grid.GetCoordinate(boardIndex);
            }

            bitPosition += 2;
            if (bitPosition == 8)
            {
                byteIndex++;
                bitPosition = 0;
            }
        }

        if (_ballPosition == nullPosition)
        { throw new ArgumentException("Ball position not found"); }

        bool isBallSurrounded = true;
        foreach (var adjacentPos in _grid.GetAdjacentCoordinates(_ballPosition))
        {
            if (GetPiece(adjacentPos) == PieceType.Empty)
            {
                isBallSurrounded = false;
                break;
            }
        }
        if (isBallSurrounded)
        {
            Winner = CurrentPlayer;
            return;
        }
        if (TurnStep == 1 || TurnStep == 3)
        {
            CheckForLegalMoves();
        }
    }

    // Helper method to get data size for a given board radius
    public static int GetSerializedSize(int radius)
    {
        // Calculate total cells in hexagonal grid of given radius
        int totalCells = 3 * radius * (radius + 1) + 1;
        // Calculate bytes needed: 1 for header + ceil(totalCells * 2 / 8) for board
        return 1 + (totalCells + 3) / 4;
    }

    public AmoeballState Clone()
    {
        var clone = new AmoeballState();
        Array.Copy(_board, clone._board, _board.Length);
        clone.CurrentPlayer = CurrentPlayer;
        clone.TurnStep = TurnStep;
        clone._ballPosition = _ballPosition;
        clone.LastMove = LastMove;
        return clone;
    }



    public IEnumerable<AmoeballState> GetNextStates()
    {
        if (TurnStep == 1 || TurnStep == 3)
        {
            // Find all valid placements
            for (int i = 0; i < _board.Length; i++)
            {
                var pos = _grid.GetCoordinate(i);
                if (IsValidPlacement(pos))
                {
                    // Check if this placement would kick the ball
                    bool willKickBall = _grid.GetDistance(pos, _ballPosition) == 1;

                    if (willKickBall)
                    {
                        // Generate a state for each possible kick target
                        foreach (var kickTarget in GetKickDestination(pos))
                        {
                            var newState = Clone();
                            newState.ApplyMove(new Move(pos, kickTarget));
                            yield return newState;
                        }
                    }
                    else
                    {
                        var newState = Clone();
                        newState.ApplyPlacement(new Move(pos));
                        yield return newState;
                    }
                }
            }
        }
        else // TurnStep == 2 (removal)
        {
            // Find all pieces that can be removed
            for (int i = 0; i < _board.Length; i++)
            {
                if (_board[i] == CurrentPlayer)
                {
                    var pos = _grid.GetCoordinate(i);
                    var newState = Clone();
                    newState.ApplyRemoval(new Move(pos));
                    yield return newState;
                }
            }
        }
    }


    public void ApplyMove(Move move)
    {
        switch (TurnStep)
        {
            case 1: // First placement
            case 3: // Second placement
                ApplyPlacement(move);
                break;

            case 2: // Removal
                ApplyRemoval(move);
                break;

            default:
                throw new InvalidOperationException($"Invalid turn step: {TurnStep}");
        }
    }

    public void CheckForLegalMoves()
    {
        bool hasValidMove = false;
        for (int i = 0; i < _board.Length; i++)
        {
            var pos = _grid.GetCoordinate(i);
            if (IsValidPlacement(pos))
            {
                hasValidMove = true;
                break;
            }
        }

        if (!hasValidMove)
        {
            Winner = CurrentPlayer == PieceType.GreenAmoeba ? PieceType.PurpleAmoeba : PieceType.GreenAmoeba;
        }
    }

    public void ApplyRemoval(Move move)
    {
        if (GetPiece(move.Position) != CurrentPlayer)
        {
            throw new InvalidOperationException("Can only remove your own pieces");
        }
        SetPiece(move.Position, PieceType.Empty);
        LastMove = move;
        TurnStep++;
        CheckForLegalMoves();
    }


    public void ApplyPlacement(Move move)
    {
        // Place the piece
        SetPiece(move.Position, CurrentPlayer);
        LastMove = move;

        // If this move includes a kick, move the ball
        if (move.KickTarget.HasValue)
        {
            if (move.KickTarget.Value == _ballPosition)
            {
                Winner = CurrentPlayer;
                return;
            }
            SetPiece(_ballPosition, PieceType.Empty);
            SetPiece(move.KickTarget.Value, PieceType.Ball);
        }

        // Update turn state if this was the second placement
        if (TurnStep == 3)
        {
            CurrentPlayer = CurrentPlayer == PieceType.GreenAmoeba ? PieceType.PurpleAmoeba : PieceType.GreenAmoeba;
            TurnStep = 1;
            CheckForLegalMoves();
        }
        else
        {
            TurnStep++;
        }
    }

    // FNV hash constants
    private const int FNV_PRIME = 16777619;
    private const int FNV_OFFSET_BASIS = -2128831035;

    public static int ComputeHash(byte[] data)
    {
        const int FNV_PRIME = 16777619;
        const int FNV_OFFSET_BASIS = -2128831035;

        int hash = FNV_OFFSET_BASIS;
        for (int i = 0; i < data.Length; i++)
        {
            hash ^= data[i];
            hash *= FNV_PRIME;
        }
        return hash;
    }

    public override int GetHashCode()
    {
        var serialized = Serialize();
        return ComputeHash(serialized);
    }

    public override bool Equals(object? obj)
    {
        if (obj is AmoeballState other)
        {
            var serialized = Serialize();
            var otherSerialized = other.Serialize();
            // First compare hashes
            if (ComputeHash(serialized) != ComputeHash(otherSerialized))
            {
                return false;
            }
            // If hashes match, do full comparison
            return serialized.SequenceEqual(otherSerialized);
        }
        return false;
    }

    public static bool operator ==(AmoeballState left, AmoeballState right)
    {
        if (ReferenceEquals(left, null))
            return ReferenceEquals(right, null);
        return left.Equals(right);
    }

    public static bool operator !=(AmoeballState left, AmoeballState right)
    {
        return !(left == right);
    }


    public struct Move
    {
        public Vector2I Position;
        public Vector2I? KickTarget;  // Only present if this move kicked the ball

        public Move(Vector2I position, Vector2I? kickTarget = null)
        {
            Position = position;
            KickTarget = kickTarget;
        }

        //public int Order()
        //{
        //    var _grid = HexGrid.Instance;
        //    int kickIndex = KickTarget.HasValue ? _grid.GetIndex(KickTarget.Value) : 0;
        //    return _grid.GetIndex(Position) * _grid.TotalCells + kickIndex;
        //}
    }

}
