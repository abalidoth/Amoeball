using Godot;
using AmoeballAI;

namespace AmoeballAIIntegration
{
	/// <summary>
	/// Abstract base class for managing C# Player instances in Godot
	/// </summary>
	[GlobalClass]
	public abstract partial class PlayerProvider : Node
	{
		/// <summary>
		/// The C# Player object managed by this provider
		/// </summary>
		protected Player? playerInstance;

		/// <summary>
		/// Current game state being processed
		/// </summary>
		private AmoeballState? currentState;

		/// <summary>
		/// Called when the node enters the scene tree
		/// </summary>
		public override void _Ready()
		{
			InitializePlayer();
			
			if (playerInstance == null)
			{
				GD.PrintErr("PlayerProvider: Failed to initialize player instance");
			}
		}

		/// <summary>
		/// Abstract method - must be implemented by derived classes
		/// This should create and return the appropriate C# Player object
		/// </summary>
		protected abstract void InitializePlayer();

		/// <summary>
		/// Process turn with a GDScript AmoeballState (passed as serialized bytes)
		/// This initializes the internal state for this turn
		/// </summary>
		/// <param name="serializedState">Byte array from GDScript AmoeballState.serialize()</param>
		public void ProcessTurn(byte[] serializedState)
		{
			if (playerInstance == null)
			{
				GD.PrintErr("PlayerProvider: No player instance available");
				return;
			}
			
			// Convert GDScript state to C# state and store it
			currentState = new AmoeballState();
			currentState.Deserialize(serializedState);
			
			// Process the turn (runs MCTS simulations, etc.)
			playerInstance.ProcessTurn(currentState);
		}

		/// <summary>
		/// Select a single move using the internal state
		/// Updates the internal state and returns move information
		/// </summary>
		/// <returns>The move that was made</returns>
		public AmoeballState.Move SelectSingleMove()
		{
			if (playerInstance == null)
			{
				GD.PrintErr("PlayerProvider: No player instance available");
				return new AmoeballState.Move();
			}
			
			if (currentState == null)
			{
				GD.PrintErr("PlayerProvider: ProcessTurn must be called before SelectSingleMove");
				return new AmoeballState.Move();
			}
			
			// Get the next state after making a move
			currentState = playerInstance.SelectSingleMove(currentState);
			
			// Return the move that was made
			return currentState.LastMove;
		}

		/// <summary>
		/// Clear the internal state
		/// </summary>
		public void ClearState()
		{
			currentState = null;
		}

		/// <summary>
		/// Cleanup when the node is removed from the scene
		/// </summary>
		public override void _ExitTree()
		{
			currentState = null;
			playerInstance = null;
		}
	}
}
