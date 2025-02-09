using static AmoeballState;

public class AmoeballMCTS
{
    public readonly OrderedGameTree _tree;
    private readonly Random _random;
    private readonly AmoeballState _initialState;
    private readonly int _maxDepth;
    private const double EXPLORATION_CONSTANT = 1.41421356237; // √2

    public AmoeballMCTS(AmoeballState initialState, int maxDepth = int.MaxValue)
    {
        _initialState = initialState;
        _tree = new OrderedGameTree(initialState);
        _random = new Random();
        _maxDepth = maxDepth;
    }

    public void RunSimulations(int simulations, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < simulations && !cancellationToken.IsCancellationRequested; i++)
        {
            var leafIndex = Select();

            // Keep expanding randomly until we hit max depth
            while (_tree.GetDepth(leafIndex) < _maxDepth)
            {
                _tree.Expand(leafIndex);
                var children = _tree.GetChildIndices(leafIndex);
                if (children.Length == 0) break;

                // Select a random child to expand next
                leafIndex = children[_random.Next(children.Length)];
            }

            var winner = SimulateFromNode(leafIndex);
            _tree.Backpropagate(leafIndex, winner);
        }
    }

    public Move GetBestMove(bool randomizeSymmetry = false)
    {
        var canonicalMove = GetBestRootMove();
        return TransformMoveToCurrentState(canonicalMove, randomizeSymmetry);
    }

    private Move TransformMoveToCurrentState(Move canonicalMove, bool randomize = false)
    {
        AmoeballState canonicalForm = _tree.GetState(0);
        // Get all possible transformations of the move from canonical form to current state
        var transformedMoves = BoardPermutations.Instance.TransformMove(
            canonicalMove,
            canonicalForm, // Canonical form
            _initialState  // Current state
        ).ToList();
        // Return either first or random transformation based on parameter
        return randomize ?
            transformedMoves[_random.Next(transformedMoves.Count)] :
            transformedMoves.First();
    }

    private int Select()
    {
        int currentIndex = 0;

        while (_tree.IsExpanded(currentIndex) &&
               _tree.GetDepth(currentIndex) < _maxDepth)
        {
            var childIndices = _tree.GetChildIndices(currentIndex);
            if (childIndices.Length == 0) break;

            currentIndex = SelectChild(currentIndex);
        }

        return currentIndex;
    }

    private int SelectChild(int nodeIndex)
    {
        var childIndices = _tree.GetChildIndices(nodeIndex);
        var currentPlayer = _tree.GetCurrentPlayer(nodeIndex);

        return childIndices.MaxBy(childIndex =>
        {
            double exploitation = _tree.GetWinRatio(childIndex, currentPlayer);
            double exploration = Math.Sqrt(Math.Log(_tree.GetParentVisits(childIndex)) /
                                        (1 + _tree.GetVisits(childIndex)));
            return exploitation + EXPLORATION_CONSTANT * exploration;
        });
    }

    private PieceType SimulateFromNode(int nodeIndex)
    {
        var state = _tree.GetState(nodeIndex).Clone();

        while (state.Winner == PieceType.Empty)
        {
            var possibleMoves = state.GetNextStates().ToList();
            if (possibleMoves.Count == 0) break;

            state = possibleMoves[_random.Next(possibleMoves.Count)];
        }

        return state.Winner;
    }

    private Move GetBestRootMove()
    {
        var (indices, moves) = _tree.GetRootEdges();
        if (indices.Length == 0)
            throw new InvalidOperationException("No moves available");

        // Select move with highest visit count
        int bestIndex = 0;
        int maxVisits = _tree.GetVisits(indices[0]);

        for (int i = 1; i < indices.Length; i++)
        {
            int visits = _tree.GetVisits(indices[i]);
            if (visits > maxVisits)
            {
                maxVisits = visits;
                bestIndex = i;
            }
        }

        return moves[bestIndex];
    }


// Method to get statistics about possible moves
public IEnumerable<(Move move, int visits, float winRatio)> GetMoveStatistics(bool randomizeSymmetry = false)
    {
        var (childIndices, canonicalMoves) = _tree.GetRootEdges();
        var rootPlayer = _tree.GetCurrentPlayer(0);

        var stats = Enumerable.Range(0, childIndices.Length)
            .Select(i => (
                canonicalMoves[i],
                _tree.GetVisits(childIndices[i]),
                _tree.GetWinRatio(childIndices[i], rootPlayer)
            ))
            .OrderByDescending(stats => stats.Item2)
            .ToList();

        // Transform moves to match current board state
        return stats.Select(stat => (
            TransformMoveToCurrentState(stat.Item1, randomizeSymmetry),
            stat.Item2,
            stat.Item3
        ));
    }

    public void SaveToFile(string filename) => _tree.SaveToFile(filename);
}
