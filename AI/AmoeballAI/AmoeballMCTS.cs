using static AmoeballState;

public class AmoeballMCTS
{
    public readonly OrderedGameTree _gameTree;
    private readonly Random _random;
    private const double ExplorationConstant = 1.41; // UCT exploration parameter
    private readonly AmoeballState _initialState;

    public AmoeballMCTS(AmoeballState initialState)
    {
        _gameTree = new OrderedGameTree(initialState);
        AmoeballState canonicalForm = _gameTree.GetState(0);
        //TransformationValidation.ValidateTransformation(initialState, canonicalForm);
        _random = new Random();
        _initialState = initialState;
    }

    public void RunIterations(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var (finalNodeIndex, winner) = SelectAndSimulate();
            _gameTree.Backpropagate(finalNodeIndex, winner);
        }
    }

    public Move FindBestMove(int simulations, bool randomizeSymmetry = false)
    {
        RunIterations(simulations);
        var canonicalMove = GetBestMove();

        // Transform the canonical move to match the current board state
        return TransformMoveToCurrentState(canonicalMove, randomizeSymmetry);
    }

    private Move TransformMoveToCurrentState(Move canonicalMove, bool randomize = false)
    {
        AmoeballState canonicalForm = _gameTree.GetState(0);
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

    private (int finalNodeIndex, PieceType winner) SelectAndSimulate()
    {
        int currentNodeIndex = 0; // Root node
        bool explorationPhase = true; // True during selection, false during simulation

        while (true)
        {
            if (!_gameTree.IsExpanded(currentNodeIndex))
            {
                _gameTree.Expand(currentNodeIndex);
                explorationPhase = false; // Switch to simulation phase
            }

            int[] childIndices;
            if (currentNodeIndex == 0)
            {
                var (indices, _) = _gameTree.GetRootEdges();
                childIndices = indices;
            }
            else
            {
                childIndices = _gameTree.GetChildIndices(currentNodeIndex);
            }

            if (childIndices.Length == 0)
            {
                // Terminal state - need to determine winner
                var state = _gameTree.GetState(currentNodeIndex);
                if (state.Winner != PieceType.Empty)
                {
                    return (currentNodeIndex, state.Winner);
                }
                // No winner set but game is over - current player loses
                var loser = _gameTree.GetCurrentPlayer(currentNodeIndex);
                return (currentNodeIndex, loser == PieceType.GreenAmoeba ?
                    PieceType.PurpleAmoeba : PieceType.GreenAmoeba);
            }

            // Select next node - use UCT during exploration, random during simulation
            int nextNodeIndex;
            if (explorationPhase)
            {
                nextNodeIndex = SelectBestUCTChild(currentNodeIndex, childIndices);
            }
            else
            {
                int randomIndex = _random.Next(childIndices.Length);
                nextNodeIndex = childIndices[randomIndex];
            }

            _gameTree.SetParent(nextNodeIndex, currentNodeIndex);
            currentNodeIndex = nextNodeIndex;
        }
    }

    private int SelectBestUCTChild(int nodeIndex, int[] childIndices)
    {
        double bestScore = double.MinValue;
        int bestChildIndex = -1;
        PieceType currentPlayer = _gameTree.GetCurrentPlayer(nodeIndex);

        foreach (int childIndex in childIndices)
        {
            double score = CalculateUCTScore(childIndex, currentPlayer);
            if (score > bestScore)
            {
                bestScore = score;
                bestChildIndex = childIndex;
            }
        }

        return bestChildIndex;
    }

    private double CalculateUCTScore(int nodeIndex, PieceType perspective)
    {
        int parentVisits = _gameTree.GetVisits(_gameTree.GetParent(nodeIndex));
        int nodeVisits = _gameTree.GetVisits(nodeIndex);

        if (nodeVisits == 0)
        {
            return double.MaxValue; // Ensures unvisited nodes are explored
        }

        double exploitation = _gameTree.GetWinRatio(nodeIndex, perspective);
        double exploration = Math.Sqrt(Math.Log(parentVisits) / nodeVisits);

        return exploitation + ExplorationConstant * exploration;
    }

    private Move GetBestMove()
    {
        var (childIndices, canonicalMoves) = _gameTree.GetRootEdges();
        int mostVisitedIndex = -1;
        int maxVisits = -1;

        for (int i = 0; i < childIndices.Length; i++)
        {
            int visits = _gameTree.GetVisits(childIndices[i]);
            if (visits > maxVisits)
            {
                maxVisits = visits;
                mostVisitedIndex = i;
            }
        }

        return canonicalMoves[mostVisitedIndex];
    }

    // Method to get statistics about possible moves
    public IEnumerable<(Move move, int visits, float winRatio)> GetMoveStatistics(bool randomizeSymmetry = false)
    {
        var (childIndices, canonicalMoves) = _gameTree.GetRootEdges();
        var rootPlayer = _gameTree.GetCurrentPlayer(0);

        var stats = Enumerable.Range(0, childIndices.Length)
            .Select(i => (
                canonicalMoves[i],
                _gameTree.GetVisits(childIndices[i]),
                _gameTree.GetWinRatio(childIndices[i], rootPlayer)
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
}