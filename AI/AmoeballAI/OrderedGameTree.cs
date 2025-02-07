using static AmoeballState;

public class OrderedGameTree
{
    private struct Node
    {
        public TransformationCache StateCache;
        public int ParentIndex;
        public int[] ChildIndices;
        public int ChildCount;
        public int Depth;
        public bool IsExpanded;
        public int Visits;
        public int GreenWins;
        public int PurpleWins;
        public PieceType CurrentPlayer;

        public Node(AmoeballState state, int parentIndex, int depth)
        {
            StateCache = new TransformationCache(state);
            ParentIndex = parentIndex;
            ChildIndices = new int[12];  // Initial capacity
            ChildCount = 0;
            Depth = depth;
            IsExpanded = false;
            Visits = 0;
            GreenWins = 0;
            PurpleWins = 0;
            CurrentPlayer = state.CurrentPlayer;
        }

        public void AddChild(int childIndex)
        {
            if (ChildIndices.Contains(childIndex)) return;
            if (ChildCount >= ChildIndices.Length)
            {
                var newIndices = new int[ChildIndices.Length * 2];
                Array.Copy(ChildIndices, newIndices, ChildIndices.Length);
                ChildIndices = newIndices;
            }

            ChildIndices[ChildCount] = childIndex;
            ChildCount++;
        }
    }

    private readonly int[] _hashTable;
    private readonly Node[] _nodes;
    private readonly int _capacity;
    private int _count;
    private const double LOAD_FACTOR_THRESHOLD = 0.7;

    // Store root moves in canonical form
    private Move[] _rootMoves;
    private int _rootMoveCount;

    public OrderedGameTree(AmoeballState initialState, int initialCapacity = 1000000)
    {
        _capacity = NextPowerOfTwo(Math.Max(initialCapacity, 16));
        _hashTable = new int[_capacity];
        Array.Fill(_hashTable, -1);  // -1 indicates empty slot
        _nodes = new Node[_capacity];
        _rootMoves = new Move[12];  // Initial capacity for root moves
        _rootMoveCount = 0;
        _count = 0;

        InsertNode(initialState, -1, 0);
    }

    private static int NextPowerOfTwo(int value)
    {
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value++;
        return value;
    }

    public int FindStateIndex(AmoeballState state)
    {
        var cache = new TransformationCache(state);
        int hash = cache.GetHashCode();
        int index = Math.Abs(hash) & (_capacity - 1);

        while (_hashTable[index] != -1)
        {
            int nodeIndex = _hashTable[index];
            if (_nodes[nodeIndex].StateCache.Contains(state))
            {
                return nodeIndex;
            }
            index = (index + 1) & (_capacity - 1);
        }

        return -1;
    }

    public void Expand(int nodeIndex)
    {
        if (nodeIndex == -1 || _nodes[nodeIndex].IsExpanded)
        {
            return;
        }

        ref Node node = ref _nodes[nodeIndex];
        var state = node.StateCache.CanonicalForm;

        foreach (var nextState in state.GetNextStates())
        {
            var existingIndex = FindStateIndex(nextState);

            if (existingIndex == -1)
            {
                var newIndex = InsertNode(
                    nextState,
                    nodeIndex,
                    node.Depth + 1
                );
                node.AddChild(newIndex);

                // If this is the root node, store the move in canonical form
                if (nodeIndex == 0)
                {
                    AddRootMove(nextState.LastMove);
                }
            }
            else
            {
                node.AddChild(existingIndex);
            }
        }

        node.IsExpanded = true;
    }

    private void AddRootMove(Move move)
    {
        if (_rootMoveCount >= _rootMoves.Length)
        {
            var newMoves = new Move[_rootMoves.Length * 2];
            Array.Copy(_rootMoves, newMoves, _rootMoves.Length);
            _rootMoves = newMoves;
        }

        _rootMoves[_rootMoveCount++] = move;
    }

    private int InsertNode(AmoeballState state, int parentIndex, int depth)
    {
        if (_count >= _capacity * LOAD_FACTOR_THRESHOLD)
        {
            throw new InvalidOperationException($"Hash table load factor exceeded threshold: {_count}/{_capacity}");
        }

        // First add to ordered array
        int insertionIndex = _count;
        _nodes[insertionIndex] = new Node(state, parentIndex, depth);

        // Then add to hash table
        int hash = _nodes[insertionIndex].StateCache.GetHashCode();
        int index = Math.Abs(hash) & (_capacity - 1);

        while (_hashTable[index] != -1)
        {
            index = (index + 1) & (_capacity - 1);
        }

        _hashTable[index] = insertionIndex;
        _count++;
        return insertionIndex;
    }

    public void Backpropagate(int nodeIndex, PieceType winner)
    {
        while (nodeIndex != -1)
        {
            ref Node node = ref _nodes[nodeIndex];
            node.Visits++;
            if (winner == PieceType.GreenAmoeba)
            {
                node.GreenWins++;
            }
            else if (winner == PieceType.PurpleAmoeba)
            {
                node.PurpleWins++;
            }
            nodeIndex = node.ParentIndex;
        }
    }

    public void SetParent(int nodeIndex, int parentIndex)
    {
        _nodes[nodeIndex].ParentIndex = parentIndex;
    }

    public (int[] indices, Move[] canonicalMoves) GetRootEdges()
    {
        ref Node rootNode = ref _nodes[0];
        var indices = new int[rootNode.ChildCount];
        var moves = new Move[_rootMoveCount];
        Array.Copy(rootNode.ChildIndices, indices, rootNode.ChildCount);
        Array.Copy(_rootMoves, moves, _rootMoveCount);
        return (indices, moves);
    }

    public int[] GetChildIndices(int nodeIndex)
    {
        if (nodeIndex == -1)
        {
            return Array.Empty<int>();
        }
        ref Node node = ref _nodes[nodeIndex];
        var indices = new int[node.ChildCount];
        Array.Copy(node.ChildIndices, indices, node.ChildCount);
        return indices;
    }

    public AmoeballState GetState(int nodeIndex)
    {
        if (nodeIndex == -1)
        {
            throw new ArgumentException("Invalid node index");
        }
        return _nodes[nodeIndex].StateCache.CanonicalForm;
    }

    public int GetParent(int nodeIndex) => _nodes[nodeIndex].ParentIndex;

    public int GetParentVisits(int nodeIndex) => GetVisits(GetParent(nodeIndex));

    public PieceType GetCurrentPlayer(int nodeIndex) => _nodes[nodeIndex].CurrentPlayer;

    public float GetWinRatio(int nodeIndex, PieceType player)
    {
        if (nodeIndex == -1 || _nodes[nodeIndex].Visits == 0)
        {
            return 0f;
        }

        ref Node node = ref _nodes[nodeIndex];
        return player == PieceType.GreenAmoeba
            ? (float)node.GreenWins / node.Visits
            : (float)node.PurpleWins / node.Visits;
    }

    public int GetVisits(int nodeIndex)
    {
        return nodeIndex != -1 ? _nodes[nodeIndex].Visits : 0;
    }

    public bool IsExpanded(int nodeIndex)
    {
        return nodeIndex != -1 && _nodes[nodeIndex].IsExpanded;
    }

    public int GetNodeCount() => _count;
}