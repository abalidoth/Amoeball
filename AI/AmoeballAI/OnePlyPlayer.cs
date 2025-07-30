
namespace AmoeballAI
{
    

    public class OnePlyPlayer : Player
    {
        private readonly Random _random = new Random();

        protected override AmoeballState SelectSingleMove(AmoeballState currentState)
        {
            var possibleStates = currentState.GetNextStates().ToList();

            if (possibleStates.Count == 0)
            {
                return currentState; // No moves available
            }

            // Look for winning moves
            foreach (var state in possibleStates)
            {
                if (state.Winner == currentState.CurrentPlayer)
                {
                    return state;
                }
            }

            // Look for moves that don't result in immediate loss
            var safeMoves = possibleStates
                .Where(state => state.Winner != GetOpponentColor(currentState.CurrentPlayer))
                .ToList();

            // If we have safe moves, choose randomly from them
            if (safeMoves.Count > 0)
            {
                return safeMoves[_random.Next(safeMoves.Count)];
            }

            // If all moves lead to loss, choose randomly from all moves
            return possibleStates[_random.Next(possibleStates.Count)];
        }

        private static PieceType GetOpponentColor(PieceType currentPlayer)
        {
            return currentPlayer == PieceType.GreenAmoeba ? PieceType.PurpleAmoeba : PieceType.GreenAmoeba;
        }
    }
}
