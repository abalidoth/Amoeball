using Godot;
using AmoeballAI;
using System.Threading.Tasks;

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

		[Signal]
		public delegate void ProcessTurnCompletedEventHandler();

		private bool _isProcessing = false;

		// Synchronous method that starts async processing
		public void ProcessTurn(byte[] serializedState)
		{
			if (_isProcessing) return;

			_isProcessing = true;
			_ = ProcessTurnAsync(serializedState);
		}


		/// <summary>
		/// Process turn with a GDScript AmoeballState (passed as serialized bytes)
		/// This initializes the internal state for this turn
		/// </summary>
		/// <param name="serializedState">Byte array from GDScript AmoeballState.serialize()</param>
		public async Task ProcessTurnAsync(byte[] serializedState)
		{
			try
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
				await playerInstance.ProcessTurn(currentState);
			}
			finally
			{
				_isProcessing = false;
				EmitSignal(SignalName.ProcessTurnCompleted);
			}
		}

		/// <summary>
		/// Select a single move using the internal state
		/// Updates the internal state and returns move information
		/// </summary>
		/// <returns>The move that was made</returns>		
		public Godot.Collections.Dictionary SelectSingleMove()
{
if (playerInstance == null)
	{
		GD.PrintErr("PlayerProvider: No player instance available");
		return new Godot.Collections.Dictionary
		{
			["Position"] = Vector2I.Zero
		};
	}
	
	if (currentState == null)
	{
		GD.PrintErr("PlayerProvider: ProcessTurn must be called before SelectSingleMove");
		return new Godot.Collections.Dictionary
		{
			["Position"] = Vector2I.Zero
		};
	}
	
	// Get the next state after making a move
	currentState = playerInstance.SelectSingleMove(currentState);
	
	// Return the move as a dictionary that GDScript can understand
	var move = currentState.LastMove;
	var result = new Godot.Collections.Dictionary
	{
		["Position"] = move.Position
	};
	
	// Only add KickTarget if it has a value
	if (move.KickTarget.HasValue)
	{
		result["KickTarget"] = move.KickTarget.Value;
	}
	
	return result;
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
