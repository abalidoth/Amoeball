public class GameTreeSystem
{
    private StateStorage _stateStorage;
    private ComponentArray<GameStateComponent> _gameStates;
    private ComponentArray<TreeStructureComponent> _structure;

    private int _capacity;
    private int _count;
    private int _rootIndex;
    private int _activeParentIndex;
    private readonly int _stateSize;

    public GameTreeSystem(int initialCapacity = 1000)
    {
        _capacity = initialCapacity;
        _stateSize = AmoeballState.GetSerializedSize(HexGrid.RADIUS);

        _stateStorage = new StateStorage(initialCapacity);
        _gameStates = new ComponentArray<GameStateComponent>(initialCapacity);
        _structure = new ComponentArray<TreeStructureComponent>(initialCapacity);

        _count = 0;
        _rootIndex = -1;
        _activeParentIndex = -1;
    }


    // Tree Structure Methods

    public int GetParent(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);
        return _structure[nodeIndex].ParentIndex;
    }

    public ArraySegment<int> GetChildren(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);
        var structure = _structure[nodeIndex];
        if (structure.ChildCount == 0)
        {
            return new ArraySegment<int>(Array.Empty<int>());
        }

        int[] childIndices = new int[structure.ChildCount];
        for (int i = 0; i < structure.ChildCount; i++)
        {
            childIndices[i] = structure.ChildStartIndex + i;
        }
        return new ArraySegment<int>(childIndices);
    }

    public void DeactivateSubtree(int rootIndex)
    {
        ValidateNodeIndex(rootIndex);

        var stack = new Stack<int>();
        stack.Push(rootIndex);

        while (stack.Count > 0)
        {
            int currentIndex = stack.Pop();
            var structure = _structure[currentIndex];
            structure.IsActive = false;
            _structure[currentIndex] = structure;

            if (structure.ChildCount > 0)
            {
                for (int i = 0; i < structure.ChildCount; i++)
                {
                    stack.Push(structure.ChildStartIndex + i);
                }
            }
        }
    }

    // Expansion and State Management

    public void Expand(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);

        var structure = _structure[nodeIndex];
        if (structure.IsExpanded)
        {
            return;
        }

        ChangeActiveParent(nodeIndex);

        var state = GetState(nodeIndex);
        foreach (var nextState in state.GetNextStates())
        {
            AddChild(nextState);
        }

        structure.IsExpanded = true;
        _structure[nodeIndex] = structure;
    }

    public bool IsExpanded(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);
        return _structure[nodeIndex].IsExpanded;
    }

    public bool IsActive(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);
        return _structure[nodeIndex].IsActive;
    }

    // Depth-related Methods

    public int GetDepth(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);
        return _structure[nodeIndex].Depth;
    }

    public int GetMaxDepth()
    {
        int maxDepth = 0;
        for (int i = 0; i < _count; i++)
        {
            var structure = _structure[i];
            if (structure.IsActive && structure.Depth > maxDepth)
            {
                maxDepth = structure.Depth;
            }
        }
        return maxDepth;
    }

    public IEnumerable<int> GetNodesAtDepth(int depth)
    {
        for (int i = 0; i < _count; i++)
        {
            var structure = _structure[i];
            if (structure.IsActive && structure.Depth == depth)
            {
                yield return i;
            }
        }
    }

    // Core Tree Operations

    public void InitializeRoot(AmoeballState rootState)
    {
        if (_count > 0)
        {
            throw new InvalidOperationException("Tree already initialized");
        }

        _rootIndex = AllocateNode();

        byte[] serializedState = rootState.Serialize();
        int stateIndex = _stateStorage.Add(serializedState);

        _gameStates[_rootIndex] = new GameStateComponent
        {
            StateIndex = stateIndex,
            CurrentPlayer = rootState.CurrentPlayer,
            LastMove = default
        };

        _structure[_rootIndex] = new TreeStructureComponent
        {
            ParentIndex = -1,
            ChildStartIndex = 0,
            ChildCount = 0,
            Depth = 0,
            IsActive = true,
            IsExpanded = false
        };

        _activeParentIndex = -1;
    }

    public int AddChild(AmoeballState childState)
    {
        if (_activeParentIndex == -1)
        {
            throw new InvalidOperationException(
                "No active parent set. Call ChangeActiveParent before adding children.");
        }

        int childIndex = AllocateNode();
        var parentStructure = _structure[_activeParentIndex];

        if (parentStructure.ChildCount == 0)
        {
            parentStructure.ChildStartIndex = childIndex;
        }
        else if (childIndex != parentStructure.ChildStartIndex + parentStructure.ChildCount)
        {
            throw new InvalidOperationException(
                $"Children must be consecutive in memory. Expected index {parentStructure.ChildStartIndex + parentStructure.ChildCount} " +
                $"but got {childIndex}");
        }

        byte[] serializedState = childState.Serialize();
        int stateIndex = _stateStorage.Add(serializedState);

        _gameStates[childIndex] = new GameStateComponent
        {
            StateIndex = stateIndex,
            CurrentPlayer = childState.CurrentPlayer,
            LastMove = childState.LastMove
        };

        _structure[childIndex] = new TreeStructureComponent
        {
            ParentIndex = _activeParentIndex,
            ChildStartIndex = 0,
            ChildCount = 0,
            Depth = parentStructure.Depth + 1,
            IsActive = true,
            IsExpanded = false
        };

        parentStructure.ChildCount++;
        _structure[_activeParentIndex] = parentStructure;

        return childIndex;
    }

    

    public void ChangeActiveParent(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);

        var structure = _structure[nodeIndex];
        if (structure.ChildCount > 0)
        {
            throw new InvalidOperationException(
                $"Cannot set node {nodeIndex} as active parent: node already has {structure.ChildCount} children");
        }

        _activeParentIndex = nodeIndex;
    }


    // State Access Methods

    public AmoeballState GetState(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);
        var state = new AmoeballState();
        byte[] serializedState = _stateStorage.GetState(_gameStates[nodeIndex].StateIndex);
        state.Deserialize(serializedState);
        return state;
    }

    public AmoeballState.Move GetLastMove(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);
        return _gameStates[nodeIndex].LastMove;
    }

    public AmoeballState.PieceType GetCurrentPlayer(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);
        return _gameStates[nodeIndex].CurrentPlayer;
    }

    // Statistics methods

    public void IncrementVisit(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);
        _stateStorage.IncrementVisit(_gameStates[nodeIndex].StateIndex);
    }

    public void AddWin(int nodeIndex, AmoeballState.PieceType winner)
    {
        ValidateNodeIndex(nodeIndex);
        _stateStorage.AddWin(_gameStates[nodeIndex].StateIndex, winner);
    }

    public int GetVisits(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);
        return _stateStorage.GetVisits(_gameStates[nodeIndex].StateIndex);
    }

    public int GetWins(int nodeIndex, AmoeballState.PieceType player)
    {
        ValidateNodeIndex(nodeIndex);
        return _stateStorage.GetWins(_gameStates[nodeIndex].StateIndex, player);
    }

    public float GetWinRatio(int nodeIndex, AmoeballState.PieceType player)
    {
        ValidateNodeIndex(nodeIndex);
        return _stateStorage.GetWinRatio(_gameStates[nodeIndex].StateIndex, player);
    }




    // Helper Methods

    private void ResizeArrays(int newCapacity)
    {
        _gameStates.Resize(newCapacity);
        _structure.Resize(newCapacity);
        _capacity = newCapacity;
    }

    private int AllocateNode()
    {
        if (_count >= _capacity)
        {
            ResizeArrays(_capacity * 2);
        }
        return _count++;
    }

    private void ValidateNodeIndex(int index)
    {
        if (!IsValidNodeIndex(index))
        {
            throw new ArgumentException($"Invalid node index: {index}");
        }
    }

    private bool IsValidNodeIndex(int index)
    {
        return index >= 0 && index < _count;
    }

    public int GetNodeCount() => _count;

    public int GetStateCount() => _stateStorage.Count;

    public int GetRootIndex() => _rootIndex != -1 ? _rootIndex :
        throw new InvalidOperationException("Tree not initialized");


}