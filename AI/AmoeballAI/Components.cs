

public class ComponentArray<T>
{
    private T[] _data;
    private int _capacity;

    public ComponentArray(int initialCapacity)
    {
        _capacity = initialCapacity;
        _data = new T[initialCapacity];
    }

    public void Resize(int newCapacity)
    {
        Array.Resize(ref _data, newCapacity);
        _capacity = newCapacity;
    }

    public T this[int index]
    {
        get => _data[index];
        set => _data[index] = value;
    }
}

public struct GameStateComponent
{
    public int StateIndex;
    public AmoeballState.PieceType CurrentPlayer;
    public AmoeballState.Move LastMove;
}

public struct TreeStructureComponent
{
    public int ParentIndex;
    public int ChildStartIndex;
    public int ChildCount;
    public int Depth;
    public bool IsActive;
    public bool IsExpanded;
}

public struct StateStatistics
{
    public int Visits;
    public int GreenWins;
    public int PurpleWins;

    public float GetWinRatio(AmoeballState.PieceType player)
    {
        if (Visits == 0) return 0f;
        return (float)GetWins(player) / Visits;
    }

    public int GetWins(AmoeballState.PieceType player) => player == AmoeballState.PieceType.GreenAmoeba ? GreenWins : PurpleWins;
}
