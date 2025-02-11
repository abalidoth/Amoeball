using System.IO;
using static AmoeballState;

public partial class OrderedGameTree
{
    // File format version for compatibility checking
    private const int FILE_VERSION = 1;

    public void SaveToFile(string filename)
    {
        using var stream = new FileStream(filename, FileMode.Create);
        using var writer = new BinaryWriter(stream);

        // Write file header and version
        writer.Write("AMOETREE"u8.ToArray());  // Magic number/identifier
        writer.Write(FILE_VERSION);

        // Write basic tree information
        writer.Write(_capacity);
        writer.Write(_count);
        writer.Write(_rootMoveCount);

        // Write root moves array
        for (int i = 0; i < _rootMoveCount; i++)
        {
            WriteMove(writer, _rootMoves[i]);
        }

        // Write nodes array
        for (int i = 0; i < _count; i++)
        {
            WriteNode(writer, _nodes[i]);
        }
    }

    private void WriteMove(BinaryWriter writer, Move move)
    {
        // Write position
        writer.Write(move.Position.X);
        writer.Write(move.Position.Y);

        // Write kick target if present
        writer.Write(move.KickTarget.HasValue);
        if (move.KickTarget.HasValue)
        {
            writer.Write(move.KickTarget.Value.X);
            writer.Write(move.KickTarget.Value.Y);
        }
    }

    private void WriteNode(BinaryWriter writer, Node node)
    {
        // Write state cache data
        var stateData = node.StateCache.CanonicalForm.Serialize();
        writer.Write(stateData.Length);
        writer.Write(stateData);

        // Write basic node data
        writer.Write(node.ParentIndex);
        writer.Write(node.ChildCount);
        writer.Write(node.Depth);
        writer.Write(node.IsExpanded);
        writer.Write(node.Visits);
        writer.Write(node.GreenWins);
        writer.Write(node.PurpleWins);
        writer.Write((byte)node.CurrentPlayer);

        // Write child indices
        for (int i = 0; i < node.ChildCount; i++)
        {
            writer.Write(node.ChildIndices[i]);
        }
    }

    public static OrderedGameTree LoadFromFile(string filename)
    {
        using var stream = new FileStream(filename, FileMode.Open);
        using var reader = new BinaryReader(stream);

        // Verify file header
        var magic = reader.ReadBytes(8).AsSpan();
        if (!magic.SequenceEqual("AMOETREE"u8))
        {
            throw new InvalidDataException("Invalid file format");
        }

        // Check version
        int version = reader.ReadInt32();
        if (version != FILE_VERSION)
        {
            throw new InvalidDataException($"Unsupported file version: {version}");
        }

        // Read basic tree information
        int capacity = reader.ReadInt32();
        int count = reader.ReadInt32();
        int rootMoveCount = reader.ReadInt32();

        // Create initial game state to construct the tree
        var initialState = new AmoeballState();
        initialState.SetupInitialPosition();
        var tree = new OrderedGameTree(initialState, capacity);

        // Read and restore root moves
        tree._rootMoveCount = rootMoveCount;
        tree._rootMoves = new Move[Math.Max(12, rootMoveCount)];
        for (int i = 0; i < rootMoveCount; i++)
        {
            tree._rootMoves[i] = ReadMove(reader);
        }

        // Read and restore nodes
        for (int i = 0; i < count; i++)
        {
            tree._nodes[i] = ReadNode(reader);
        }

        tree._count = count;

        // Reconstruct hash table
        Array.Fill(tree._hashTable, -1);  // Reset hash table
        for (int i = 0; i < count; i++)
        {
            // Get hash for the node's state
            int hash = tree._nodes[i].StateCache.GetHashCode();
            int index = Math.Abs(hash) & (capacity - 1);

            // Find next empty slot using linear probing
            while (tree._hashTable[index] != -1)
            {
                index = (index + 1) & (capacity - 1);
            }

            tree._hashTable[index] = i;
        }

        return tree;
    }

    private static Move ReadMove(BinaryReader reader)
    {
        // Read position
        var x = reader.ReadInt32();
        var y = reader.ReadInt32();
        var position = new Godot.Vector2I(x, y);

        // Read kick target if present
        bool hasKickTarget = reader.ReadBoolean();
        Godot.Vector2I? kickTarget = null;
        if (hasKickTarget)
        {
            x = reader.ReadInt32();
            y = reader.ReadInt32();
            kickTarget = new Godot.Vector2I(x, y);
        }

        return new Move(position, kickTarget);
    }

    private static Node ReadNode(BinaryReader reader)
    {
        // Read state data
        int stateDataLength = reader.ReadInt32();
        byte[] stateData = reader.ReadBytes(stateDataLength);

        // Create state and deserialize
        var state = new AmoeballState();
        state.Deserialize(stateData);

        // Read basic node data
        int parentIndex = reader.ReadInt32();
        int childCount = reader.ReadInt32();
        int depth = reader.ReadInt32();
        bool isExpanded = reader.ReadBoolean();
        int visits = reader.ReadInt32();
        int greenWins = reader.ReadInt32();
        int purpleWins = reader.ReadInt32();
        PieceType currentPlayer = (PieceType)reader.ReadByte();

        // Create and populate node
        var node = new Node(state, parentIndex, depth)
        {
            IsExpanded = isExpanded,
            Visits = visits,
            GreenWins = greenWins,
            PurpleWins = purpleWins
        };

        // Read child indices
        for (int i = 0; i < childCount; i++)
        {
            
            node.AddChild(reader.ReadInt32());
        }

        return node;
    }
}