using Godot;
using static AmoeballAI.AmoeballState;

namespace AmoeballAI
{
    public partial class OrderedGameTree
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
            int index = (int)(unchecked((uint)hash) & (uint)(_capacity - 1));

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

            InsertIntoHashTable(insertionIndex);

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

        public int GetDepth(int nodeIndex) => _nodes[nodeIndex].Depth;

        public int GetNodeCount() => _count;

        private void InsertIntoHashTable(int nodeIndex)
        {
            int hash = _nodes[nodeIndex].StateCache.GetHashCode();
            int index = (int)(unchecked((uint)hash) & (uint)(_capacity - 1));

            while (_hashTable[index] != -1)
            {
                index = (index + 1) & (_capacity - 1);
            }

            _hashTable[index] = nodeIndex;
        }

        private void RebuildRootMoves()
        {
            _rootMoves = new Move[12];
            _rootMoveCount = 0;

            // Get the root node and its canonical next states
            ref Node rootNode = ref _nodes[0];
            var rootState = rootNode.StateCache.CanonicalForm;
            var nextStates = rootState.GetNextStates().ToList();

            // Process children in order to maintain correspondence with ChildIndices
            for (int i = 0; i < rootNode.ChildCount; i++)
            {
                int childIndex = rootNode.ChildIndices[i];
                var childState = GetState(childIndex);

                // Find matching state from GetNextStates
                var matchingState = nextStates.First(state =>
                    new TransformationCache(state).Contains(childState));

                AddRootMove(matchingState.LastMove);
            }
        }

        public void Prune(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= _count)
            {
                throw new ArgumentException("Invalid node index");
            }

            // Nothing to prune if we're keeping the whole tree
            if (nodeIndex == 0)
            {
                return;
            }

            // Step 1: Find all descendants and record their parents
            var descendantNodes = new HashSet<int>();
            var newParents = new Dictionary<int, int>();  // node -> parent in pruned tree
            var queue = new Queue<int>();
            queue.Enqueue(nodeIndex);
            descendantNodes.Add(nodeIndex);

            while (queue.Count > 0)
            {
                int currentIndex = queue.Dequeue();
                ref Node currentNode = ref _nodes[currentIndex];

                for (int i = 0; i < currentNode.ChildCount; i++)
                {
                    int childIndex = currentNode.ChildIndices[i];
                    if (descendantNodes.Add(childIndex))  // If this is a new descendant
                    {
                        newParents[childIndex] = currentIndex;  // Current node is its parent
                        queue.Enqueue(childIndex);
                    }
                }
            }

            // Step 2: Create mapping from old indices to new indices
            var oldToNewIndex = new Dictionary<int, int>();
            oldToNewIndex[nodeIndex] = 0;  // Put root first
            int newIndex = 1;
            foreach (int oldIndex in descendantNodes.Where(x => x != nodeIndex))
            {
                oldToNewIndex[oldIndex] = newIndex++;
            }

            // Step 3: Compact the nodes array
            var newNodes = new Node[_capacity];
            foreach (var kvp in oldToNewIndex)
            {
                int oldIndex = kvp.Key;
                int newIdx = kvp.Value;

                // Copy the node
                newNodes[newIdx] = _nodes[oldIndex];

                // Set parent (root has no entry in newParents)
                newNodes[newIdx].ParentIndex = oldIndex == nodeIndex ?
                    -1 : oldToNewIndex[newParents[oldIndex]];

                // Update child indices
                for (int i = 0; i < newNodes[newIdx].ChildCount; i++)
                {
                    newNodes[newIdx].ChildIndices[i] = oldToNewIndex[newNodes[newIdx].ChildIndices[i]];
                }
            }
            

            // Step 4: Update the nodes array and count
            Array.Copy(newNodes, _nodes, _capacity);
            _count = descendantNodes.Count;

            // Step 5: Reset hash table and rebuild it
            Array.Fill(_hashTable, -1);
            for (int i = 0; i < _count; i++)
            {
                InsertIntoHashTable(i);
            }

            // Step 6: Rebuild root moves for the new root
            RebuildRootMoves();

            // Step 7: Validate connectivity
            ValidateTreeStructure();
        }

        public void ValidateTreeStructure()
        {
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(0);  // Start from root

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();

                // Check for invalid indices
                if (current >= _count || current < 0)
                {
                    throw new InvalidOperationException($"Invalid node index detected: {current}");
                }

                visited.Add(current);
                ref Node node = ref _nodes[current];

                // Validate parent reference
                if (node.ParentIndex >= _count || node.ParentIndex < -1)
                {
                    throw new InvalidOperationException(
                        $"Node {current} has invalid parent index: {node.ParentIndex}");
                }

                // Validate depth relative to parent
                if (node.ParentIndex != -1)
                {
                    ref Node parent = ref _nodes[node.ParentIndex];
                    if (node.Depth != parent.Depth + 1)
                    {
                        throw new InvalidOperationException(
                            $"Node {current} has invalid depth {node.Depth}. " +
                            $"Parent node {node.ParentIndex} has depth {parent.Depth}");
                    }
                }

                // Validate children
                if (node.ChildCount > node.ChildIndices.Length)
                {
                    throw new InvalidOperationException(
                        $"Node {current} has invalid child count: {node.ChildCount} > {node.ChildIndices.Length}");
                }

                for (int i = 0; i < node.ChildCount; i++)
                {
                    int childIndex = node.ChildIndices[i];

                    // Check for invalid child indices
                    if (childIndex >= _count || childIndex < 0)
                    {
                        throw new InvalidOperationException(
                            $"Node {current} has invalid child index: {childIndex}");
                    }

                    // Add unvisited children to queue
                    if (!visited.Contains(childIndex))
                    {
                        queue.Enqueue(childIndex);
                    }
                }
            }

            // Check that we visited all nodes (graph is fully connected)
            if (visited.Count != _count)
            {
                var unreachable = new HashSet<int>(Enumerable.Range(0, _count));
                unreachable.ExceptWith(visited);
                throw new InvalidOperationException(
                    $"Graph has unreachable nodes: {string.Join(", ", unreachable)}");
            }
        }
        public AmoeballState PopState()
        {
            int bestNodeIndex = 0;
            int maxVisits = -1;
            int nodeCount = GetNodeCount();
            int targetDepth = GetDepth(0) + 1;

            // Linear scan through all nodes
            for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
            {
                if (GetDepth(nodeIndex) == targetDepth)
                {
                    int visits = GetVisits(nodeIndex);
                    if (visits > maxVisits)
                    {
                        maxVisits = visits;
                        bestNodeIndex = nodeIndex;
                    }
                }
            }

            if (maxVisits <= 0) throw new InvalidOperationException("No nodes visited at target depth.");
            // Return the most visited state at the target depth
            var resultState = GetState(bestNodeIndex);
            Prune(bestNodeIndex);
            return resultState;
        }
    }
}