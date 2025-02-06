using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AmoeballState;

public readonly struct SerializedState : IEquatable<SerializedState>
{
    private readonly byte[] _data;
    private readonly int _hash;

    public SerializedState(AmoeballState state)
    {
        _data = state.Serialize();
        _hash = ComputeHash(_data);
    }

    public SerializedState(byte[] data)
    {
        _data = (byte[])data.Clone();
        _hash = ComputeHash(_data);
    }

    public AmoeballState Deserialize()
    {
        var state = new AmoeballState();
        state.Deserialize(_data);
        return state;
    }

    public override bool Equals(object? obj)
    {
        return obj is SerializedState other && Equals(other);
    }

    public bool Equals(SerializedState other)
    {
        // First compare hashes for quick inequality check
        if (_hash != other._hash)
        {
            return false;
        }
        // If hashes match, do full comparison
        return _data.SequenceEqual(other._data);
    }

    public override int GetHashCode()
    {
        return _hash;
    }

    public static bool operator ==(SerializedState left, SerializedState right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SerializedState left, SerializedState right)
    {
        return !(left == right);
    }
}