using static AmoeballAI.AmoeballState;

namespace AmoeballAI
{
    public static class AmoeballMCTS
    {
        private static readonly Random _random = new Random();
        private const double EXPLORATION_CONSTANT = 1.41421356237; // √2

        public static void RunSimulations(OrderedGameTree tree, int simulations, int maxDepth = int.MaxValue, CancellationToken cancellationToken = default)
        {
            int trueMaxDepth = maxDepth + tree.GetDepth(0);
            for (int i = 0; i < simulations && !cancellationToken.IsCancellationRequested; i++)
            {
                var leafIndex = Select(tree, trueMaxDepth);

                // Keep expanding randomly until we hit max depth
                while (tree.GetDepth(leafIndex) < trueMaxDepth)
                {
                    tree.Expand(leafIndex);
                    var children = tree.GetChildIndices(leafIndex);
                    if (children.Length == 0) break;

                    // Select a random child to expand next
                    leafIndex = children[_random.Next(children.Length)];
                }

                var winner = SimulateFromNode(tree, leafIndex);
                tree.Backpropagate(leafIndex, winner);
            }
        }

        public static Move GetBestMove(OrderedGameTree tree, AmoeballState currentState, bool randomize = false) => TransformMoveToCurrentState(currentState, tree.GetState(0), GetBestRootMove(tree), randomize);

        private static Move TransformMoveToCurrentState(AmoeballState currentState, AmoeballState canonicalState, Move canonicalMove, bool randomize = false)
        {
            // Get all possible transformations of the move from canonical form to current state
            var transformedMoves = BoardPermutations.Instance.TransformMove(
                canonicalMove,
                canonicalState,
                currentState  // Current state
            ).ToList();
            // Return either first or random transformation based on parameter
            return randomize ?
                transformedMoves[_random.Next(transformedMoves.Count)] :
                transformedMoves.First();
        }

        private static int Select(OrderedGameTree tree, int maxDepth)
        {
            int currentIndex = 0;

            while (tree.IsExpanded(currentIndex) &&
                   tree.GetDepth(currentIndex) < maxDepth)
            {
                var childIndices = tree.GetChildIndices(currentIndex);
                if (childIndices.Length == 0) break;

                var childIndex = SelectChild(tree, currentIndex);
                tree.SetParent(childIndex, currentIndex);
                currentIndex = childIndex;

            }

            return currentIndex;
        }

        public static int SelectChild(OrderedGameTree tree, int nodeIndex)
        {
            var childIndices = tree.GetChildIndices(nodeIndex);
            var currentPlayer = tree.GetCurrentPlayer(nodeIndex);

            return childIndices.MaxBy(childIndex =>
            {
                double exploitation = tree.GetWinRatio(childIndex, currentPlayer);
                double exploration = Math.Sqrt(Math.Log(tree.GetParentVisits(childIndex)) /
                                            (1 + tree.GetVisits(childIndex)));
                return exploitation + EXPLORATION_CONSTANT * exploration;
            });
        }

        public static PieceType SimulateFromNode(OrderedGameTree tree, int nodeIndex)
        {
            var state = tree.GetState(nodeIndex).Clone();

            while (state.Winner == PieceType.Empty)
            {
                var possibleMoves = state.GetNextStates().ToList();
                if (possibleMoves.Count == 0) break;

                state = possibleMoves[_random.Next(possibleMoves.Count)];
            }

            return state.Winner;
        }

        public static Move GetBestRootMove(OrderedGameTree tree)
        {
            var (indices, moves) = tree.GetRootEdges();
            if (indices.Length == 0)
                throw new InvalidOperationException("No moves available");

            // Select move with highest visit count
            int bestIndex = 0;
            int maxVisits = tree.GetVisits(indices[0]);

            for (int i = 1; i < indices.Length; i++)
            {
                int visits = tree.GetVisits(indices[i]);
                if (visits > maxVisits)
                {
                    maxVisits = visits;
                    bestIndex = i;
                }
            }

            return moves[bestIndex];
        }


        // Method to get statistics about possible moves
        public static IEnumerable<(Move move, int visits, float winRatio)> GetMoveStatistics(OrderedGameTree tree, AmoeballState initialState, bool randomizeSymmetry = false)
        {
            var (childIndices, canonicalMoves) = tree.GetRootEdges();
            var rootPlayer = tree.GetCurrentPlayer(0);

            var stats = Enumerable.Range(0, childIndices.Length)
                .Select(i => (
                    canonicalMoves[i],
                    tree.GetVisits(childIndices[i]),
                    tree.GetWinRatio(childIndices[i], rootPlayer)
                ))
                .OrderByDescending(stats => stats.Item2)
                .ToList();

            // Transform moves to match current board state
            return stats.Select(stat => (
                TransformMoveToCurrentState(initialState, tree.GetState(0), stat.Item1, randomizeSymmetry),
                stat.Item2,
                stat.Item3
            ));
        }
    
    }
    }
