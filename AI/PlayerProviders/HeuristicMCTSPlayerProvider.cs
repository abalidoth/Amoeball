using Godot;
using System;
using AmoeballAI;

namespace AmoeballAIIntegration

{
	/// <summary>
	/// Concrete PlayerProvider for HeuristicMCTSPlayer with ProximityToBall heuristic
	/// </summary>
	[GlobalClass]
	public partial class HeuristicMCTSPlayerProvider : PlayerProvider
	{
		// MCTS Settings
		[ExportGroup("MCTS Settings")]
		[Export] public float TurnLengthSeconds { get; set; } = 3.0f;
		[Export] public int MaxDepth { get; set; } = 9;
		[Export] public bool Verbose { get; set; } = false;

		// Heuristic Settings
		[ExportGroup("Heuristic Settings")]
		[Export] public float HeuristicWeight { get; set; } = 10.0f;
		[Export(PropertyHint.Range, "0,1,0.01")] 
		public float InitialPlayoutHeuristicUsage { get; set; } = 0.8f;
		[Export(PropertyHint.Range, "0,1,0.01")] 
		public float SimulationHeuristicUsage { get; set; } = 0.5f;

		/// <summary>
		/// Initialize the HeuristicMCTSPlayer with ProximityToBall heuristic
		/// </summary>
		protected override void InitializePlayer()
		{
			// Always use ProximityToBall heuristic
			HeuristicFunction heuristicFunction = HeuristicPlayer.ProximityToBallHeuristic;
			
			// Create the HeuristicMCTSPlayer instance
			playerInstance = new HeuristicMCTSPlayer(
				TimeSpan.FromSeconds(TurnLengthSeconds),
				heuristicFunction,
				HeuristicWeight,
				InitialPlayoutHeuristicUsage,
				SimulationHeuristicUsage,
				MaxDepth,
				Verbose
			);
			
			if (playerInstance == null)
			{
				GD.PrintErr("HeuristicMCTSPlayerProvider: Failed to create HeuristicMCTSPlayer instance");
				return;
			}
			
			if (Verbose)
			{
				GD.Print("HeuristicMCTSPlayer initialized with ProximityToBall heuristic:");
				GD.Print($"  Turn length: {TurnLengthSeconds}s");
				GD.Print($"  Max depth: {MaxDepth}");
				GD.Print($"  Heuristic weight: {HeuristicWeight}");
				GD.Print($"  Initial playout heuristic usage: {InitialPlayoutHeuristicUsage}");
				GD.Print($"  Simulation heuristic usage: {SimulationHeuristicUsage}");
			}
		}

		/// <summary>
		/// Get tree statistics for debugging
		/// </summary>
		public (int nodeCount, int totalSimulations) GetTreeStats()
		{
			if (playerInstance is HeuristicMCTSPlayer mctsPlayer)
			{
				return mctsPlayer.GetTreeStats();
			}
			
			return (0, 0);
		}

		/// <summary>
		/// Get the heuristic value for the current root position
		/// </summary>
		public float GetRootHeuristicValue()
		{
			if (playerInstance is HeuristicMCTSPlayer mctsPlayer)
			{
				return mctsPlayer.GetRootHeuristicValue();
			}
			
			return float.NaN;
		}
	}
}
