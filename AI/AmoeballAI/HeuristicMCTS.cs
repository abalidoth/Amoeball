namespace AmoeballAI
{
    /// <summary>
    /// Enhanced Monte Carlo Tree Search that works with HeuristicGameTree
    /// </summary>
    public class HeuristicMCTS : MCTS
    {
        // Configuration parameters
        private readonly float _heuristicWeight = 10.0f;
        private readonly float _initialPlayoutHeuristicUsage = 0.8f;
        private readonly float _simulationHeuristicUsage = 0.5f;

        public HeuristicMCTS(
            float heuristicWeight = 10.0f,
            float initialPlayoutHeuristicUsage = 0.8f,
            float simulationHeuristicUsage = 0.5f)
        {
            _heuristicWeight = heuristicWeight;
            _initialPlayoutHeuristicUsage = initialPlayoutHeuristicUsage;
            _simulationHeuristicUsage = simulationHeuristicUsage;
        }

        /// <summary>
        /// Runs MCTS simulations with heuristic guidance
        /// </summary>
        public static void RunSimulations(
            HeuristicGameTree tree,
            int simulations,
            int maxDepth = int.MaxValue,
            CancellationToken cancellationToken = default)
        {
            if (!tree.HasHeuristic)
            {
                throw new InvalidOperationException("HeuristicGameTree must have heuristic initialized.");
            }

            var instance = new HeuristicMCTS();
            instance.RunSimulationsInternal(tree, simulations, maxDepth, cancellationToken);
        }

        /// <summary>
        /// Runs MCTS simulations with custom heuristic parameters
        /// </summary>
        public static void RunSimulations(
            HeuristicGameTree tree,
            int simulations,
            float heuristicWeight = 10.0f,
            float playoutHeuristicProbability = 0.8f,
            float expansionHeuristicProbability = 0.5f,
            int maxDepth = int.MaxValue,
            CancellationToken cancellationToken = default)
        {
            if (!tree.HasHeuristic)
            {
                throw new InvalidOperationException("HeuristicGameTree must have heuristic initialized.");
            }

            var instance = new HeuristicMCTS(
                heuristicWeight,
                playoutHeuristicProbability,
                expansionHeuristicProbability);

            instance.RunSimulationsInternal(tree, simulations, maxDepth, cancellationToken);
        }

        /// <summary>
        /// Overrides the selection phase to incorporate the heuristic
        /// </summary>
        protected override int SelectChildForSearch(OrderedGameTree tree, int nodeIndex)
        {
            var heuristicTree = tree as HeuristicGameTree;
            if (heuristicTree == null || !heuristicTree.HasHeuristic)
                return base.SelectChildForSearch(tree, nodeIndex);

            var childIndices = tree.GetChildIndices(nodeIndex);
            var currentPlayer = tree.GetCurrentPlayer(nodeIndex);

            return childIndices.MaxBy(childIndex =>
            {
                // Standard UCT term
                double exploitation = tree.GetWinRatio(childIndex, currentPlayer);
                double exploration = Math.Sqrt(Math.Log(tree.GetParentVisits(childIndex)) /
                                          (1 + tree.GetVisits(childIndex)));

                // Heuristic influence - decreases as visits increase
                double heuristicInfluence = 0;
                if (tree.GetVisits(childIndex) < 20) // Only apply to less-visited nodes
                {
                    float heuristicValue = heuristicTree.GetHeuristicValue(childIndex);
                    double decayFactor = Math.Max(0, _heuristicWeight / (1 + tree.GetVisits(childIndex)));
                    heuristicInfluence = heuristicValue * decayFactor;
                }

                return exploitation + EXPLORATION_CONSTANT * exploration + heuristicInfluence;
            });
        }

        /// <summary>
        /// Overrides the simulation child selection to use the heuristic
        /// </summary>
        protected override int SelectChildForPlayout(OrderedGameTree tree, int nodeIndex)
        {
            var heuristicTree = tree as HeuristicGameTree;
            if (heuristicTree == null || !heuristicTree.HasHeuristic)
                return base.SelectChildForPlayout(tree, nodeIndex);

            var childIndices = tree.GetChildIndices(nodeIndex);
            if (childIndices.Length == 0)
            {
                return -1;
            }

            // Use heuristic with probability, otherwise random
            if (_random.NextDouble() < _initialPlayoutHeuristicUsage)
            {
                // Use tree's heuristic values
                return childIndices.MaxBy(index => heuristicTree.GetHeuristicValue(index));
            }
            else
            {
                // Random selection
                return childIndices[_random.Next(childIndices.Length)];
            }
        }

        /// <summary>
        /// Overrides the simulation to use heuristic-guided playouts
        /// </summary>
        protected override PieceType Simulate(OrderedGameTree tree, int nodeIndex)
        {
            var heuristicTree = tree as HeuristicGameTree;
            if (heuristicTree == null || !heuristicTree.HasHeuristic)
                return base.Simulate(tree, nodeIndex);

            var state = tree.GetState(nodeIndex).Clone();
            HeuristicFunction heuristicEval = heuristicTree.HeuristicFunction;

            while (state.Winner == PieceType.Empty)
            {
                var nextStates = state.GetNextStates().ToList();
                if (nextStates.Count == 0) break;

                // Use heuristic with probability
                if (_random.NextDouble() < _simulationHeuristicUsage)
                {
                    // Evaluate next states with the same heuristic function
                    var currentPlayer = state.CurrentPlayer;
                    var evaluatedStates = nextStates
                        .Select(ns => (state: ns, value: heuristicEval(ns, currentPlayer)))
                        .OrderByDescending(item => item.value)
                        .ToList();

                    // Select from top 20% of moves with some randomization
                    int topCount = Math.Max(1, nextStates.Count / 5);
                    int selectedIndex = _random.Next(topCount);
                    state = evaluatedStates[selectedIndex].state;
                }
                else
                {
                    // Random selection
                    state = nextStates[_random.Next(nextStates.Count)];
                }
            }

            return state.Winner;
        }
    }
}
