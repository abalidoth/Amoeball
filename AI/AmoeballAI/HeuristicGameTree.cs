using Godot;
using System.Diagnostics.CodeAnalysis;
using static AmoeballAI.AmoeballState;

namespace AmoeballAI

{
    /// <summary>
    /// Delegate for heuristic evaluation functions
    /// </summary>
    /// <param name="state">The game state to evaluate</param>
    /// <param name="perspective">The player perspective to evaluate from</param>
    /// <returns>A score for the position</returns>
    public delegate float HeuristicFunction(AmoeballState state, PieceType perspective);
    /// <summary>
    /// Extension of OrderedGameTree with integrated heuristic evaluation capabilities
    /// </summary>
    public class HeuristicGameTree : OrderedGameTree
    {
        

        // Heuristic data members
        private HeuristicFunction? _heuristicEval;
        private float[]? _heuristicValues;
        private bool _heuristicsInitialized = false;

        /// <summary>
        /// Constructor with immediate heuristic initialization
        /// </summary>
        public HeuristicGameTree(AmoeballState initialState, HeuristicFunction heuristicEval, int initialCapacity = 1000000)
            : base(initialState, initialCapacity)
        {
            InitializeHeuristic(heuristicEval);
        }

        /// <summary>
        /// Constructor without heuristic initialization for deferred setup
        /// </summary>
        public HeuristicGameTree(AmoeballState initialState, int initialCapacity = 1000000)
            : base(initialState, initialCapacity)
        {
            // Heuristic will be initialized later
        }

        /// <summary>
        /// Initializes the heuristic evaluation system
        /// </summary>
        public void InitializeHeuristic(HeuristicFunction heuristicEval)
        {
            _heuristicEval = heuristicEval ?? throw new ArgumentNullException(nameof(heuristicEval));
            _heuristicValues = new float[_capacity];
            Array.Fill(_heuristicValues, float.NaN); // Use NaN to indicate uncalculated values
            _heuristicsInitialized = true;

            // Calculate heuristic for root node
            GetHeuristicValue(0);
        }

        /// <summary>
        /// Determines if heuristics are initialized for this tree
        /// </summary>
        [MemberNotNullWhen(true, nameof(_heuristicEval), nameof(_heuristicValues),nameof(HeuristicFunction))]
        public bool HasHeuristic => _heuristicsInitialized;

        /// <summary>
        /// Gets the heuristic function used by this tree
        /// </summary>
        public HeuristicFunction? HeuristicFunction => _heuristicEval;

        /// <summary>
        /// Gets the heuristic value for a node, calculating it if necessary
        /// </summary>
        public float GetHeuristicValue(int nodeIndex)
        {
            if (!HasHeuristic)
                throw new InvalidOperationException("Heuristics not initialized. Call InitializeHeuristic first.");

            if (nodeIndex < 0 || nodeIndex >= _count)
                throw new ArgumentOutOfRangeException(nameof(nodeIndex));

            // Return cached value if available
            if (!float.IsNaN(_heuristicValues[nodeIndex]))
                return _heuristicValues[nodeIndex];

            // Calculate and cache the heuristic value
            var state = GetState(nodeIndex);
            var perspective = GetCurrentPlayer(nodeIndex);

            float value = _heuristicEval(state, perspective);
            _heuristicValues[nodeIndex] = value;

            return value;
        }

        /// <summary>
        /// Clears cached heuristic values
        /// </summary>
        public void ClearHeuristicCache()
        {
            if (HasHeuristic)
            {
                Array.Fill(_heuristicValues, float.NaN);
            }
        }

        /// <summary>
        /// Override InsertNode to calculate heuristic for new nodes
        /// </summary>
        protected override int InsertNode(AmoeballState state, int parentIndex, int depth)
        {
            // Use base implementation to insert the node
            int newNodeIndex = base.InsertNode(state, parentIndex, depth);

            // Calculate heuristic if initialized
            if (HasHeuristic && newNodeIndex < _heuristicValues.Length)
            {
                try
                {
                    GetHeuristicValue(newNodeIndex);
                }
                catch (Exception)
                {
                    // Ignore errors in heuristic calculation - the value will remain NaN
                    // and will be calculated on demand later
                }
            }

            return newNodeIndex;
        }

        /// <summary>
        /// Override Expand to ensure heuristic capacity
        /// </summary>
        public override void Expand(int nodeIndex)
        {
            base.Expand(nodeIndex);

            // Ensure heuristic array has sufficient capacity
            EnsureHeuristicCapacity();
        }

        /// <summary>
        /// Override Prune to update heuristic cache using the index mapping
        /// </summary>
        public new Dictionary<int, int> Prune(int nodeIndex)
        {
            // Use base implementation to get the mapping
            var oldToNewIndex = base.Prune(nodeIndex);

            // Update heuristic cache using the mapping
            if (HasHeuristic)
            {
                var newValues = new float[_capacity];
                Array.Fill(newValues, float.NaN);

                // Copy existing heuristic values using the mapping
                foreach (var kvp in oldToNewIndex)
                {
                    int oldIndex = kvp.Key;
                    int newIndex = kvp.Value;

                    if (!float.IsNaN(_heuristicValues[oldIndex]))
                    {
                        newValues[newIndex] = _heuristicValues[oldIndex];
                    }
                }

                _heuristicValues = newValues;
            }

            return oldToNewIndex;
        }

        /// <summary>
        /// Ensures the heuristic values array has sufficient capacity
        /// </summary>
        private void EnsureHeuristicCapacity()
        {
            if (HasHeuristic && _heuristicValues.Length < _capacity)
            {
                var newValues = new float[_capacity];
                Array.Fill(newValues, float.NaN);
                Array.Copy(_heuristicValues, newValues, _heuristicValues.Length);
                _heuristicValues = newValues;
            }
        }
    }
}
