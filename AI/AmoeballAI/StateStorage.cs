
public class StateStorage
{
    // Core state storage
    private List<byte[]> _states;
    private List<StateStatistics> _statistics;
    private Dictionary<StateHash, int> _stateToIndex;

    public StateStorage(int initialCapacity = 1000)
    {
        _states = new List<byte[]>(initialCapacity);
        _statistics = new List<StateStatistics>(initialCapacity);
        _stateToIndex = new Dictionary<StateHash, int>(initialCapacity);
    }

    // Returns the index of the state in the ordered set
    public int Add(byte[] serializedState)
    {
        var hash = new StateHash(serializedState);
        if (_stateToIndex.TryGetValue(hash, out int existingIndex))
        {
            return existingIndex;
        }

        int newIndex = _states.Count;
        _states.Add((byte[])serializedState.Clone());
        _statistics.Add(new StateStatistics());
        _stateToIndex[hash] = newIndex;
        return newIndex;
    }

    public byte[] GetState(int index)
    {
        ValidateIndex(index);
        return _states[index];
    }

    // Statistics methods
    public void IncrementVisit(int stateIndex)
    {
        ValidateIndex(stateIndex);
        var stats = _statistics[stateIndex];
        stats.Visits++;
        _statistics[stateIndex] = stats;
    }

    public void AddWin(int stateIndex, AmoeballState.PieceType winner)
    {
        ValidateIndex(stateIndex);
        var stats = _statistics[stateIndex];
        if (winner == AmoeballState.PieceType.GreenAmoeba)
            stats.GreenWins++;
        else
            stats.PurpleWins++;
        _statistics[stateIndex] = stats;
    }

    public StateStatistics GetStatistics(int stateIndex)
    {
        ValidateIndex(stateIndex);
        return _statistics[stateIndex];
    }

    public int GetVisits(int stateIndex)
    {
        ValidateIndex(stateIndex);
        return _statistics[stateIndex].Visits;
    }

    public int GetWins(int stateIndex, AmoeballState.PieceType player)
    {
        ValidateIndex(stateIndex);
        return _statistics[stateIndex].GetWins(player);
    }

    public float GetWinRatio(int stateIndex, AmoeballState.PieceType player)
    {
        ValidateIndex(stateIndex);
        return _statistics[stateIndex].GetWinRatio(player);
    }

    private void ValidateIndex(int index)
    {
        if (index < 0 || index >= _states.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
    }

    public int Count => _states.Count;
}

// StateHash struct remains unchanged
public readonly struct StateHash : IEquatable<StateHash>
{
    private readonly int _hash;
    private readonly byte[] _state;

    public StateHash(byte[] state)
    {
        _state = state;
        _hash = ComputeHash(state);
    }

    private static int ComputeHash(byte[] state)
    {
        unchecked
        {
            const int p = 16777619;
            int hash = (int)2166136261;

            for (int i = 0; i < state.Length; i++)
            {
                hash = (hash ^ state[i]) * p;
            }

            hash += hash << 13;
            hash ^= hash >> 7;
            hash += hash << 3;
            hash ^= hash >> 17;
            hash += hash << 5;

            return hash;
        }
    }

    public bool Equals(StateHash other)
    {
        if (_hash != other._hash) return false;
        if (_state.Length != other._state.Length) return false;

        for (int i = 0; i < _state.Length; i++)
        {
            if (_state[i] != other._state[i]) return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
        => obj is StateHash other && Equals(other);

    public override int GetHashCode() => _hash;
}