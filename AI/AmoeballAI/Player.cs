using static AmoeballAI.AmoeballState;

namespace AmoeballAI
{

    public abstract class Player
    {
        private int _gamesPlayed;
        private int _gamesWon;
        protected PieceType _playerColor;

        public bool GameOver { get; private set; }
        public int GamesPlayed => _gamesPlayed;
        public PieceType Color => _playerColor;
        public float WinRate => _gamesPlayed > 0 ? (float)_gamesWon / _gamesPlayed : 0f;

        public AmoeballState PlayTurn(AmoeballState currentState)
        {
            if (_playerColor == PieceType.Empty)
            {
                _playerColor = currentState.CurrentPlayer;
            }
            else if (_playerColor != currentState.CurrentPlayer)
                throw new InvalidOperationException($"{_playerColor} called on {currentState.CurrentPlayer}'s turn.");

            if (currentState.TurnStep != 1)
                throw new InvalidOperationException($"Player turn begun at step {currentState.TurnStep}");

            

            var resultState = currentState.Clone();
            ProcessTurn(resultState);


            for (int step = 0; step < 3 && resultState.Winner == PieceType.Empty; step++)
            {
                resultState = SelectSingleMove(resultState);
            }

            GameOver = (resultState.Winner != PieceType.Empty);
            if (GameOver && resultState.Winner == _playerColor)
            {
                _gamesPlayed++;
                _gamesWon++;
            }

            return resultState;
        }

        public void NotifyLoss()
        {
            _gamesPlayed++;
        }

        public void NotifyWin()
        {
            _gamesPlayed++;
            _gamesWon++;
        }

        protected virtual void ProcessTurn(AmoeballState currentState)
        {        }

        protected abstract AmoeballState SelectSingleMove(AmoeballState currentState);
    }
}