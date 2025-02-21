
namespace AmoeballAI
{

    public class RandomPlayer : Player
    {
        private readonly Random _random;

        public RandomPlayer(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        protected override AmoeballState SelectSingleMove(AmoeballState currentState)
        {
            
            var nextStates = currentState.GetNextStates().ToList();
            // Select a random next state
            return nextStates[_random.Next(nextStates.Count)];

        }
    }
}


