namespace AmoeballAI
{
    /// <summary>
    /// Base class for Monte Carlo Tree Search implementations with default behavior
    /// </summary>
    public class MCTS
    {
        protected static readonly Random _random = new Random();
        protected const double EXPLORATION_CONSTANT = 1.41421356237; // √2

        /// <summary>
        /// Runs MCTS simulations on the provided tree
        /// </summary>
        public static void RunSimulations(OrderedGameTree tree, int simulations, int maxDepth = int.MaxValue, CancellationToken cancellationToken = default)
        {
            var instance = new MCTS();
            instance.RunSimulationsInternal(tree, simulations, maxDepth, cancellationToken);
        }

        /// <summary>
        /// Internal instance method that can be overridden by derived classes
        /// </summary>
        protected virtual void RunSimulationsInternal(OrderedGameTree tree, int simulations, int maxDepth = int.MaxValue, CancellationToken cancellationToken = default)
        {
            int trueMaxDepth = maxDepth + tree.GetDepth(0);
            for (int i = 0; i < simulations && !cancellationToken.IsCancellationRequested; i++)
            {
                var leafIndex = Select(tree, trueMaxDepth);

                if (!tree.IsExpanded(leafIndex))
                {
                    Expand(tree, leafIndex);
                }

                int selectedChildIndex = -1;
                if (tree.GetChildIndices(leafIndex).Length > 0)
                {
                    selectedChildIndex = SelectChildForPlayout(tree, leafIndex);
                }

                var simulationStartIndex = selectedChildIndex != -1 ? selectedChildIndex : leafIndex;
                var winner = Simulate(tree, simulationStartIndex);
                tree.Backpropagate(simulationStartIndex, winner);
            }
        }

        /// <summary>
        /// Selects a leaf node to expand using UCB
        /// </summary>
        protected virtual int Select(OrderedGameTree tree, int maxDepth)
        {
            int currentIndex = 0;

            while (tree.IsExpanded(currentIndex) &&
                  tree.GetDepth(currentIndex) < maxDepth)
            {
                var childIndices = tree.GetChildIndices(currentIndex);
                if (childIndices.Length == 0) break;

                var childIndex = SelectChildForSearch(tree, currentIndex);
                tree.SetParent(childIndex, currentIndex);
                currentIndex = childIndex;
            }

            return currentIndex;
        }

        /// <summary>
        /// Expands a node by generating its children.
        /// This is where move ordering could be applied in the future.
        /// </summary>
        protected virtual void Expand(OrderedGameTree tree, int nodeIndex)
        {
            if (tree.IsExpanded(nodeIndex))
            {
                return;
            }

            // This is where ordered expansion would happen
            // Currently just expands all children in default order
            tree.Expand(nodeIndex);

            // Future enhancement: Sort children indices based on some heuristic
            // For example:
            // 1. Get all child indices
            // 2. Score each child using a heuristic
            // 3. Reorder the indices based on scores
        }

        /// <summary>
        /// Selects which child to explore during the search phase using UCB formula
        /// </summary>
        protected virtual int SelectChildForSearch(OrderedGameTree tree, int nodeIndex)
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

        /// <summary>
        /// Selects which child to use for the simulation/playout phase.
        /// By default selects a random child, but can be overridden to use move ordering.
        /// </summary>
        protected virtual int SelectChildForPlayout(OrderedGameTree tree, int nodeIndex)
        {
            var childIndices = tree.GetChildIndices(nodeIndex);
            if (childIndices.Length == 0)
            {
                return -1;
            }

            // Default: select random child for playout
            return childIndices[_random.Next(childIndices.Length)];

            // Future enhancement: Select best child based on some heuristic
            // Example: return childIndices.OrderByDescending(i => EvaluateForPlayout(tree, i)).First();
        }
        /// <summary>
        /// Simulates from the given state to determine a winner using random playouts
        /// </summary>
        protected virtual PieceType Simulate(OrderedGameTree tree, int nodeIndex)
        {
            var state = tree.GetState(nodeIndex).Clone();

            while (state.Winner == PieceType.Empty)
            {
                var possibleMoves = state.GetNextStates().ToList();
                if (possibleMoves.Count == 0) break;

                // Here too, move ordering could be applied in the future
                state = possibleMoves[_random.Next(possibleMoves.Count)];
            }

            return state.Winner;
        }

        /// <summary>
        /// Gets the best move from the tree
        /// </summary>
        public static Move GetBestMove(OrderedGameTree tree, AmoeballState currentState, bool randomize = false)
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

            return TransformMoveToCurrentState(currentState, tree.GetState(0), moves[bestIndex], randomize);
        }

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

        /// <summary>
        /// Gets statistics about possible moves
        /// </summary>
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