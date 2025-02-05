using System;
using System.Collections.Generic;
using System.Linq;
using static AmoeballState;

public class AmoeballMCTS
{
    private readonly OrderedGameTree _gameTree;
    private readonly Random _random;
    private const double ExplorationConstant = 1.41; // UCT exploration parameter

    public AmoeballMCTS(AmoeballState initialState)
    {
        _gameTree = new OrderedGameTree(initialState);
        _random = new Random();
    }

    public void RunIterations(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var (finalNodeIndex, winner) = SelectAndSimulate();
            _gameTree.Backpropagate(finalNodeIndex, winner);
        }
    }

    public Move FindBestMove(int simulations)
    {
        RunIterations(simulations);
        return GetBestMove();
    }

    private (int finalNodeIndex, AmoeballState.PieceType winner) SelectAndSimulate()
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

            var (childIndices, moves) = _gameTree.GetChildEdges(currentNodeIndex);
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
        AmoeballState.PieceType currentPlayer = _gameTree.GetCurrentPlayer(nodeIndex);

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

    private double CalculateUCTScore(int nodeIndex, AmoeballState.PieceType perspective)
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

    public Move GetBestMove()
    {
        var (childIndices, moves) = _gameTree.GetChildEdges(0);
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

        return moves[mostVisitedIndex];
    }

    // Method to get statistics about possible moves
    public IEnumerable<(Move move, int visits, float winRatio)> GetMoveStatistics()
    {
        var (childIndices, moves) = _gameTree.GetChildEdges(0);
        var rootPlayer = _gameTree.GetCurrentPlayer(0);

        return Enumerable.Range(0, childIndices.Length).Select(i => (
            moves[i],
            _gameTree.GetVisits(childIndices[i]),
            _gameTree.GetWinRatio(childIndices[i], rootPlayer)
        )).OrderByDescending(stats => stats.Item2);
    }
}