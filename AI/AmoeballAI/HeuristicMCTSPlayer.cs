namespace AmoeballAI
{
	public class HeuristicMCTSPlayer : Player
	{
		// Configuration parameters
		private readonly TimeSpan _turnLength;
		private readonly int _maxDepth;
		private readonly bool _verbose;

		// Heuristic configuration
		public HeuristicFunction Heuristic { get; set; }
		public float HeuristicWeight { get; set; }
		public float InitialPlayoutHeuristicUsage { get; set; }
		public float SimulationHeuristicUsage { get; set; }

		// Game state tracking
		private HeuristicGameTree? _gameTree;

		/// <summary>
		/// Creates a new HeuristicMCTSPlayer with the specified parameters
		/// </summary>
		/// <param name="turnLength">Time allowed for each turn</param>
		/// <param name="heuristic">Heuristic evaluation function (defaults to ProximityToBallHeuristic)</param>
		/// <param name="heuristicWeight">Weight of heuristic in UCB formula (default 10.0)</param>
		/// <param name="initialPlayoutHeuristicUsage">Probability of using heuristic for initial playout selection (default 0.8)</param>
		/// <param name="simulationHeuristicUsage">Probability of using heuristic during simulation (default 0.5)</param>
		/// <param name="maxDepth">Maximum search depth (default int.MaxValue)</param>
		/// <param name="verbose">Whether to output debug information</param>
		public HeuristicMCTSPlayer(
			TimeSpan turnLength,
			HeuristicFunction? heuristic = null,
			float heuristicWeight = 10.0f,
			float initialPlayoutHeuristicUsage = 0.8f,
			float simulationHeuristicUsage = 0.5f,
			int maxDepth = int.MaxValue,
			bool verbose = false)
		{
			_turnLength = turnLength;
			_maxDepth = maxDepth;
			_verbose = verbose;

			Heuristic = heuristic ?? HeuristicPlayer.ProximityToBallHeuristic;
			HeuristicWeight = heuristicWeight;
			InitialPlayoutHeuristicUsage = initialPlayoutHeuristicUsage;
			SimulationHeuristicUsage = simulationHeuristicUsage;
		}

		public override void ProcessTurn(AmoeballState currentState)
		{
			// Initialize or update game tree
			if (_gameTree == null)
			{
				_gameTree = new HeuristicGameTree(currentState, Heuristic);
			}
			else
			{
				// Try to find current state in existing tree
				int stateIndex = _gameTree.FindStateIndex(currentState);
				if (stateIndex == -1)
				{
					// State not found, create new tree
					_gameTree = new HeuristicGameTree(currentState, Heuristic);
				}
				else
				{
					// Prune tree to current state
					_gameTree.Prune(stateIndex);

					// Re-initialize heuristic if it was changed
					if (_gameTree.HeuristicFunction != Heuristic)
					{
						_gameTree.InitializeHeuristic(Heuristic);
					}
				}
			}

			var cancellationTokenSource = new CancellationTokenSource();
			cancellationTokenSource.CancelAfter(_turnLength);

			var initialSimCount = _gameTree.GetVisits(0);
			var initialNodeCount = _gameTree.GetNodeCount();

			try
			{
				// Run MCTS with heuristic guidance
				HeuristicMCTS.RunSimulations(
					_gameTree,
					int.MaxValue,
					HeuristicWeight,
					InitialPlayoutHeuristicUsage,
					SimulationHeuristicUsage,
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
				HeuristicMCTS.RunSimulations(
					_gameTree,
					int.MaxValue,
					HeuristicWeight,
					InitialPlayoutHeuristicUsage,
					SimulationHeuristicUsage,
					0,
					cancellationTokenSource.Token
				);
			}

			if (_verbose)
			{
				var finalSimCount = _gameTree.GetVisits(0);
				var finalNodeCount = _gameTree.GetNodeCount();
				var rootHeuristicValue = _gameTree.GetHeuristicValue(0);

				Console.WriteLine($"{Color} MCTS completed {finalSimCount - initialSimCount} simulations " +
								$"(tree size: {finalNodeCount} nodes, +{finalNodeCount - initialNodeCount} this turn)");
				Console.WriteLine($"  Root heuristic value: {rootHeuristicValue:F3}");
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

		/// <summary>
		/// Gets current tree statistics
		/// </summary>
		/// <returns>Tuple of (node count, total simulations)</returns>
		public (int nodeCount, int totalSimulations) GetTreeStats()
		{
			if (_gameTree == null)
			{
				return (0, 0);
			}

			return (_gameTree.GetNodeCount(), _gameTree.GetVisits(0));
		}

		/// <summary>
		/// Gets the heuristic value for the current root position
		/// </summary>
		/// <returns>The heuristic value or NaN if no tree exists</returns>
		public float GetRootHeuristicValue()
		{
			if (_gameTree == null)
			{
				return float.NaN;
			}

			return _gameTree.GetHeuristicValue(0);
		}
	}
}