public readonly struct TransformationCache
{
    private readonly SerializedState[] _allForms;
    private readonly int _canonicalIndex;

    public TransformationCache(AmoeballState state)
    {
        var baseForm = new SerializedState(state);
        _allForms = new SerializedState[BoardPermutations.TransformationCount];
        _allForms[0] = baseForm;

        // Generate all transformed states using permutations
        for (int i = 1; i < BoardPermutations.TransformationCount; i++)
        {
            var transformed = new SerializedState(
                BoardPermutations.Instance.ApplyPermutation(baseForm.Data.ToArray(), i));
            _allForms[i] = transformed;
        }

        // Find canonical form (state with minimum hash)
        _canonicalIndex = 0;
        int minHash = _allForms[0].GetHashCode();

        for (int i = 1; i < _allForms.Length; i++)
        {
            int hash = _allForms[i].GetHashCode();
            if (hash < minHash)
            {
                minHash = hash;
                _canonicalIndex = i;
            }
        }
    }

    /// <summary>
    /// Gets all equivalent forms of the state under symmetry
    /// </summary>
    public ReadOnlySpan<SerializedState> AllForms => _allForms;

    /// <summary>
    /// Gets the canonical form of the state
    /// </summary>
    public AmoeballState CanonicalForm => _allForms[_canonicalIndex].Deserialize();

    /// <summary>
    /// Checks if a state is equivalent under any transformation to the cached state
    /// </summary>
    public bool Contains(SerializedState state)
    {
        for (int i = 0; i < _allForms.Length; i++)
        {
            if (_allForms[i].Equals(state))
                return true;
        }
        return false;
    }

    public bool Contains(AmoeballState state) => Contains(new SerializedState(state));

    public override bool Equals(object? obj)
    {
        return obj is TransformationCache other && Equals(other);
    }

    public bool Equals(TransformationCache other)
    {
        return _allForms[_canonicalIndex].Equals(other._allForms[other._canonicalIndex]);
    }

    public override int GetHashCode()
    {
        return _allForms[_canonicalIndex].GetHashCode();
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
