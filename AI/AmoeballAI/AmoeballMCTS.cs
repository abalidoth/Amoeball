using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Linq;
using static AmoeballState;
using Godot;

public class AmoeballMCTS
{
    private readonly OrderedGameTree _gameTree;
    private readonly Random _random;
    private const double ExplorationConstant = 1.41; // UCT exploration parameter
    private const int DefaultMaxDepth = 100; // Reasonable default for Amoeball

    private readonly int _maxDepth;

    public AmoeballMCTS(AmoeballState initialState, int maxDepth = DefaultMaxDepth)
    {
        _gameTree = new OrderedGameTree(initialState);
        _random = new Random();
        _maxDepth = maxDepth;
    }

    public Move FindBestMove(int simulations)
    {
        for (int i = 0; i < simulations; i++)
        {
            var (finalNodeIndex, winner) = SelectAndSimulate();
            _gameTree.BackPropagate(finalNodeIndex, winner);
        }

        return GetBestMove();
    }

    private (int finalNodeIndex, PieceType winner) SelectAndSimulate()
    {
        int currentNodeIndex = 0; // Root node
        bool explorationPhase = true; // True during selection, false during simulation
        int depth = 0;

        while (depth < _maxDepth)
        {
            if (!_gameTree.IsExpanded(currentNodeIndex))
            {
                _gameTree.Expand(currentNodeIndex);
                explorationPhase = false; // Switch to simulation phase
            }

            var(childIndices, legalMoves) = _gameTree.GetChildEdges(currentNodeIndex);
            if (childIndices.Length == 0)
            {
                // Terminal state - need to deserialize to determine winner
                var state = _gameTree.GetState(currentNodeIndex);
                if (state.Winner != AmoeballState.PieceType.Empty)
                {
                    return (currentNodeIndex, state.Winner);
                }
                // No winner set but game is over - current player loses
                var loser = _gameTree.GetCurrentPlayer(currentNodeIndex);
                return (currentNodeIndex, loser == AmoeballState.PieceType.GreenAmoeba ?
                    AmoeballState.PieceType.PurpleAmoeba : AmoeballState.PieceType.GreenAmoeba);
            }

            // Select next node - use UCT during exploration, random during simulation
            int randomChild = _random.Next(childIndices.Length);
            (int nextNodeIndex, Move lastMove) = explorationPhase ?
                SelectBestUCTChild(currentNodeIndex) :
                (childIndices[randomChild], legalMoves[randomChild]);

            _gameTree.SetParent(nextNodeIndex, currentNodeIndex, lastMove);
            currentNodeIndex = nextNodeIndex;
            depth++;
        }

        // If we hit the depth limit, count it as a draw by returning no winner
        return (currentNodeIndex, AmoeballState.PieceType.Empty);
    }

    private (int, Move) SelectBestUCTChild(int nodeIndex)
    {
        double bestScore = double.MinValue;
        int bestChildIndex = -1;
        int childIndex;
        Move bestMove = default;

        var (childIndices, legalMoves) = _gameTree.GetChildEdges(nodeIndex);
        AmoeballState.PieceType currentPlayer = _gameTree.GetCurrentPlayer(nodeIndex);

        for (int i = 0; i<= childIndices.Length; i++)
        {
            childIndex = childIndices[i];
            double score = CalculateUCTScore(childIndex, currentPlayer);
            if (score > bestScore)
            {
                bestScore = score;
                bestChildIndex = childIndex;
                bestMove = legalMoves[i];
            }
        }

        return (bestChildIndex, bestMove);
    }

    private double CalculateUCTScore(int nodeIndex, PieceType perspective)
    {
        int parentVisits = _gameTree.GetParentVisits(nodeIndex);
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
        var rootChildren = _gameTree.GetChildIndices(0);
        int mostVisitedIndex = -1;
        int maxVisits = -1;

        foreach (int childIndex in rootChildren)
        {
            int visits = _gameTree.GetVisits(childIndex);
            if (visits > maxVisits)
            {
                maxVisits = visits;
                mostVisitedIndex = childIndex;
            }
        }

        return _gameTree.GetMove(mostVisitedIndex);
    }

    // Method to get statistics about possible moves
    public IEnumerable<(Move, int, double)> GetMoveStatistics()
    {
        var rootChildren = _gameTree.GetChildIndices(0);
        var rootPlayer = _gameTree.GetCurrentPlayer(0);

        return (IEnumerable<(Move,int, double)>)rootChildren.Select(childIndex => (
            _gameTree.GetMove(childIndex),
            _gameTree.GetVisits(childIndex),
            _gameTree.GetWinRatio(childIndex, rootPlayer)
        )).OrderByDescending(stats => stats.Item2);
    }
}
