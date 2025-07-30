
namespace AmoeballAI
{

    public class HeuristicPlayer : Player
    {
        /// <summary>
        /// The heuristic function used to evaluate positions
        /// </summary>
        public HeuristicFunction Heuristic { get; set; }

        /// <summary>
        /// Whether to maximize (true) or minimize (false) the heuristic value
        /// </summary>
        public bool Maximize { get; set; } = true;

        /// <summary>
        /// Whether to output debug information
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Creates a new heuristic player with the specified evaluation function
        /// </summary>
        /// <param name="heuristic">Function to evaluate positions</param>
        /// <param name="maximize">Whether to maximize (true) or minimize (false) the heuristic</param>
        /// <param name="verbose">Whether to output debug information</param>
        public HeuristicPlayer(HeuristicFunction? heuristic = null, bool maximize = true, bool verbose = false)
        {
            Heuristic = heuristic ?? ProximityToBallHeuristic;
            Maximize = maximize;
            Verbose = verbose;
        }

        protected override AmoeballState SelectSingleMove(AmoeballState currentState)
        {
            var possibleMoves = currentState.GetNextStates().ToList();

            if (possibleMoves.Count == 0)
            {
                return currentState; // No moves available
            }

            // No need to check for immediate wins separately
            // as the heuristic will assign float.MaxValue to winning states

            // Evaluate all possible moves using the heuristic function
            var evaluatedMoves = new List<(AmoeballState state, float score)>();
            foreach (var state in possibleMoves)
            {
                float score = Heuristic(state, currentState.CurrentPlayer);
                evaluatedMoves.Add((state, score));
            }

            // Sort by score based on maximize/minimize setting
            if (Maximize)
            {
                evaluatedMoves.Sort((a, b) => b.score.CompareTo(a.score));
            }
            else
            {
                evaluatedMoves.Sort((a, b) => a.score.CompareTo(b.score));
            }

            if (Verbose && evaluatedMoves.Count > 0)
            {
                var bestScore = evaluatedMoves[0].score;
                Console.WriteLine($"{Color} chose move with {(Maximize ? "max" : "min")} score: {bestScore}");
            }

            // Return the state with the best score
            return evaluatedMoves[0].state;
        }

        /// <summary>
        /// Heuristic based on proximity to the ball, with closer pieces being more valuable
        /// For each friendly piece, adds 1/distance to the ball
        /// For each opposing piece, subtracts 1/distance to the ball
        /// Winning states get float.MaxValue, losing states get float.MinValue
        /// </summary>
        public static float ProximityToBallHeuristic(AmoeballState state, PieceType perspective)
        {
            // Check for win/loss states first
            if (state.Winner != PieceType.Empty)
            {
                return state.Winner == perspective ? float.MaxValue : float.MinValue;
            }

            float score = 0;
            var grid = HexGrid.Instance;
            var ballPosition = state.GetBallPosition();
            var opponentColor = GetOpponentColor(perspective);

            for (int i = 0; i < grid.TotalCells; i++)
            {
                var position = grid.GetCoordinate(i);
                var piece = state.GetPiece(position);

                if (piece != PieceType.Empty && piece != PieceType.Ball)
                {
                    int distance = grid.GetDistance(position, ballPosition);
                    if (distance > 0) // Avoid division by zero
                    {
                        float value = 1.0f / distance;

                        if (piece == perspective)
                        {
                            score += value; // Friendly piece
                        }
                        else if (piece == opponentColor)
                        {
                            score -= value; // Opposing piece
                        }
                    }
                }
            }

            return score;
        }

        private static PieceType GetOpponentColor(PieceType currentPlayer)
        {
            return currentPlayer == PieceType.GreenAmoeba ? PieceType.PurpleAmoeba : PieceType.GreenAmoeba;
        }
    }
}
