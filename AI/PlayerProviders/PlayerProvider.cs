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
		/// Wrapper for Player.ProcessTurn
		/// </summary>
		public void ProcessTurn(AmoeballState currentState)
		{
			if (playerInstance == null)
			{
				GD.PrintErr("PlayerProvider: No player instance available");
				return;
			}
			
			playerInstance.ProcessTurn(currentState);
		}

		/// <summary>
		/// Wrapper for Player.SelectSingleMove
		/// </summary>
		public AmoeballState SelectSingleMove(AmoeballState currentState)
		{
			if (playerInstance == null)
			{
				GD.PrintErr("PlayerProvider: No player instance available");
				return currentState;
			}
			
			return playerInstance.SelectSingleMove(currentState);
		}

		/// <summary>
		/// Cleanup when the node is removed from the scene
		/// </summary>
		public override void _ExitTree()
		{
			playerInstance = null;
		}
	}
}
