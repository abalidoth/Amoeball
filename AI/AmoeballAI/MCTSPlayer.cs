
namespace AmoeballAI
{
    public class MCTSPlayer : Player
    {
        // Configuration parameters
        private readonly TimeSpan _turnLength;
        private readonly int _maxDepth;
        private readonly bool _verbose;


        // Game state tracking
        private OrderedGameTree? _gameTree;


        public MCTSPlayer(TimeSpan turnLength, int maxDepth = int.MaxValue, bool verbose = false)
        {
            _turnLength = turnLength;
            _maxDepth = maxDepth;
            _verbose = verbose;
        }

        public override void ProcessTurn(AmoeballState currentState)
        {
            // Initialize or update game tree
            if (_gameTree == null)
            {
                _gameTree = new OrderedGameTree(currentState);
            }
            else
            {
                // Try to find current state in existing tree
                int stateIndex = _gameTree.FindStateIndex(currentState);
                if (stateIndex == -1)
                {
                    // State not found, create new tree
                    _gameTree = new OrderedGameTree(currentState);
                }
                else
                {
                    // Prune tree to current state
                    _gameTree.Prune(stateIndex);
                }
            }

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(_turnLength);

            var initialSimCount = _gameTree.GetVisits(0);
            var initialNodeCount = _gameTree.GetNodeCount();

            

            try
            {
                // Try running MCTS with normal depth
                MCTS.RunSimulations(
                    _gameTree,
                    int.MaxValue,
                    _maxDepth,
                    cancellationTokenSource.Token
                );
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Hash table load factor exceeded"))
            {
                if (_verbose)
                {
                    Console.WriteLine("Hash table full, continuing with MaxDepth=0");
                }

                // If hash table is full, continue with MaxDepth=0 to prevent further expansion
                MCTS.RunSimulations(
                    _gameTree,
                    int.MaxValue,
                    0,
                    cancellationTokenSource.Token
                );
            }

            if (_verbose)
            {
                var finalSimCount = _gameTree.GetVisits(0);
                var finalNodeCount = _gameTree.GetNodeCount();
                Console.WriteLine($"{Color} MCTS completed {finalSimCount - initialSimCount} simulations " +
                                $"(tree size: {finalNodeCount} nodes, +{finalNodeCount - initialNodeCount} this turn)");
            }

        }

        public override AmoeballState SelectSingleMove(AmoeballState currentState)
        {
            return _gameTree!.PopState();
        }

        protected override void OnGameComplete()
        {
            // Clear the game tree after each game to prevent memory buildup
            _gameTree = null;
        }



        public (int nodeCount, int totalSimulations) GetTreeStats()
        {
            if (_gameTree == null)
            {
                return (0, 0);
            }

            return (_gameTree.GetNodeCount(), _gameTree.GetVisits(0));
        }
    }
}