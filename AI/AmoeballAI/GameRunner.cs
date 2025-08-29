namespace AmoeballAI
{
    public class GameRunner
    {
        private readonly Player _greenPlayer;
        private readonly Player _purplePlayer;
        private readonly bool _verbose;

        public GameRunner(Player greenPlayer, Player purplePlayer, bool verbose = false)
        {
            _greenPlayer = greenPlayer;
            _purplePlayer = purplePlayer;
            _verbose = verbose;
        }

        public async Task RunGames(int numberOfGames)
        {
            for (int i = 0; i < numberOfGames; i++)
            {
                if (_verbose)
                {
                    Console.WriteLine($"\nStarting Game {i + 1}");
                }

                var winner = await PlaySingleGame();

                if (_verbose)
                {
                    Console.WriteLine($"Game {i + 1} Winner: {(winner == PieceType.GreenAmoeba ? "Green" : "Purple")}");
                }

                // Print running statistics every 10 games or on the last game
                if ((i + 1) % 10 == 0 || i == numberOfGames - 1)
                {
                    PrintStats();
                }
            }
        }

        private async Task<PieceType> PlaySingleGame()
        {
            var state = new AmoeballState();
            state.SetupInitialPosition();

            int Turn = 0;

            while (true)
            {

                Turn++;
                // if (_verbose) Console.WriteLine("Running Turn {0}: {1} pieces on board", Turn, state.PieceCount());

                // Green's turn
                state = await _greenPlayer.PlayTurn(state);
                if (_greenPlayer.GameOver)
                {
                    if (state.Winner == PieceType.GreenAmoeba)
                    {
                        _purplePlayer.NotifyLoss();
                    }
                    else
                    {
                        _purplePlayer.NotifyWin();
                    }
                    return state.Winner;
                }


                // Purple's turn
                state = await _purplePlayer.PlayTurn(state);
                if (_purplePlayer.GameOver)
                {
                    if (state.Winner == PieceType.PurpleAmoeba)
                    {
                        _greenPlayer.NotifyLoss();
                    }
                    else
                    {
                        _greenPlayer.NotifyWin();
                    }
                    return state.Winner;
                }
            }
        }

        private void PrintStats()
        {
            Console.WriteLine("\nCurrent Statistics:");
            Console.WriteLine($"Green Player: Games: {_greenPlayer.GamesPlayed}, Win Rate: {_greenPlayer.WinRate:F3}");
            Console.WriteLine($"Purple Player: Games: {_purplePlayer.GamesPlayed}, Win Rate: {_purplePlayer.WinRate:F3}");
        }
    }
}