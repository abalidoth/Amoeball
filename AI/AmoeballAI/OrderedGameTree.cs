using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static AmoeballState;

public class OrderedGameTree
{
    private struct Node
    {
        public byte[] State;
        public int ParentIndex;
        public int[] ChildIndices;
        public Move[] LegalMoves;  // Stores the move that leads to each child
        public int ChildCount;
        public int Depth;
        public bool IsExpanded;
        public int Visits;
        public int GreenWins;
        public int PurpleWins;
        public PieceType CurrentPlayer;

        public Node(byte[] state, int parentIndex, PieceType currentPlayer, int depth)
        {
            State = state;
            ParentIndex = parentIndex;
            ChildIndices = new int[12];  // Initial capacity
            LegalMoves = new Move[12];   // Same size as ChildIndices
            ChildCount = 0;
            Depth = depth;
            IsExpanded = false;
            Visits = 0;
            GreenWins = 0;
            PurpleWins = 0;
            CurrentPlayer = currentPlayer;
        }

        public void AddChild(int childIndex, Move move)
        {
            if (ChildCount >= ChildIndices.Length)
            {
                var newIndices = new int[ChildIndices.Length * 2];
                var newMoves = new Move[LegalMoves.Length * 2];
                Array.Copy(ChildIndices, newIndices, ChildIndices.Length);
                Array.Copy(LegalMoves, newMoves, LegalMoves.Length);
                ChildIndices = newIndices;
                LegalMoves = newMoves;
            }

            ChildIndices[ChildCount] = childIndex;
            LegalMoves[ChildCount] = move;
            ChildCount++;
        }
    }

    private readonly int[] _hashTable;
    private readonly Node[] _nodes;
    private readonly int _capacity;
    private int _count;
    private const double LOAD_FACTOR_THRESHOLD = 0.7;

    public OrderedGameTree(AmoeballState initialState, int initialCapacity = 100000)
    {
        _capacity = NextPowerOfTwo(Math.Max(initialCapacity, 16));
        _hashTable = new int[_capacity];
        Array.Fill(_hashTable, -1);  // -1 indicates empty slot
        _nodes = new Node[_capacity];

        InsertNode(initialState.Serialize(), -1, initialState.CurrentPlayer, 0);
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
        return FindNodeIndex(state.Serialize());
    }

    public void Expand(int nodeIndex)
    {
        if (nodeIndex == -1 || _nodes[nodeIndex].IsExpanded)
        {
            return;
        }

        ref Node node = ref _nodes[nodeIndex];
        var state = new AmoeballState();
        state.Deserialize(node.State);

        foreach (var nextState in state.GetNextStates())
        {
            var serializedNext = nextState.Serialize();
            var existingIndex = FindNodeIndex(serializedNext);

            if (existingIndex == -1)
            {
                var newIndex = InsertNode(
                    serializedNext,
                    nodeIndex,
                    nextState.CurrentPlayer,
                    node.Depth + 1
                );
                node.AddChild(newIndex, nextState.LastMove);
            }
            else
            {
                node.AddChild(existingIndex, nextState.LastMove);
            }
        }

        node.IsExpanded = true;
    }

    private int InsertNode(byte[] state, int parentIndex, PieceType currentPlayer, int depth)
    {
        if (_count >= _capacity * LOAD_FACTOR_THRESHOLD)
        {
            throw new InvalidOperationException($"Hash table load factor exceeded threshold: {_count}/{_capacity}");
        }

        // First add to ordered array
        int insertionIndex = _count;
        _nodes[insertionIndex] = new Node(state, parentIndex, currentPlayer, depth);

        // Then add to hash table
        int hash = ComputeStateHash(state);
        int index = Math.Abs(hash) & (_capacity - 1);

        while (_hashTable[index] != -1)
        {
            index = (index + 1) & (_capacity - 1);
        }

        _hashTable[index] = insertionIndex;
        _count++;
        return insertionIndex;
    }


    private int FindNodeIndex(byte[] state)
    {
        int hash = ComputeStateHash(state);
        int index = Math.Abs(hash) & (_capacity - 1);

        while (_hashTable[index] != -1)
        {
            int nodeIndex = _hashTable[index];
            if (_nodes[nodeIndex].State.SequenceEqual(state))
            {
                return nodeIndex;
            }
            index = (index + 1) & (_capacity - 1);
        }

        return -1;
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

    public (int[] indices, Move[] moves) GetChildEdges(int nodeIndex)
    {
        if (nodeIndex == -1)
        {
            return (Array.Empty<int>(), Array.Empty<Move>());
        }
        ref Node node = ref _nodes[nodeIndex];

        var indices = new int[node.ChildCount];
        var moves = new Move[node.ChildCount];
        Array.Copy(node.ChildIndices, indices, node.ChildCount);
        Array.Copy(node.LegalMoves, moves, node.ChildCount);
        return (indices, moves);
    }

    public AmoeballState GetState(int nodeIndex)
    {
        if (nodeIndex == -1)
        {
            throw new ArgumentException("Invalid node index");
        }
        var state = new AmoeballState();
        state.Deserialize(_nodes[nodeIndex].State);
        return state;
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

    private static int ComputeStateHash(byte[] state)
    {
        // FNV-1a hash
        const int FNV_PRIME = 16777619;
        const int FNV_OFFSET_BASIS = -2128831035;

        int hash = FNV_OFFSET_BASIS;
        for (int i = 0; i < state.Length; i++)
        {
            hash ^= state[i];
            hash *= FNV_PRIME;
        }
        return hash;
    }

    public int[] GetNodesAtDepth(int targetDepth)
    {
        // Simply scan the nodes array up to _count
        int count = 0;
        for (int i = 0; i < _count; i++)
        {
            if (_nodes[i].Depth == targetDepth)
            {
                count++;
            }
        }

        int[] result = new int[count];
        int resultIndex = 0;
        for (int i = 0; i < _count; i++)
        {
            if (_nodes[i].Depth == targetDepth)
            {
                result[resultIndex++] = i;
            }
        }

        return result;
    }

    public double GetAverageProbeLength()
    {
        long totalProbes = 0;
        int nonEmptySlots = 0;

        for (int i = 0; i < _capacity; i++)
        {
            int stateIndex = _hashTable[i];
            if (stateIndex != -1 && _nodes[stateIndex].State != null)
            {
                int hash = Math.Abs(ComputeStateHash(_nodes[stateIndex].State));
                int idealIndex = hash & (_capacity - 1);
                int actualIndex = i;

                int probeLength = actualIndex >= idealIndex ?
                    actualIndex - idealIndex :
                    _capacity - idealIndex + actualIndex;

                totalProbes += probeLength + 1;
                nonEmptySlots++;
            }
        }

        return nonEmptySlots > 0 ? (double)totalProbes / nonEmptySlots : 0;
    }
}