using System.Diagnostics;
using static AmoeballState;

public class MCTSBenchmark
{
    public static void RunTest()
    {
        Console.WriteLine("Starting MCTS Convergence Benchmark");
        Console.WriteLine("==================================");

        // Create initial game state
        var state = new AmoeballState();
        state.SetupInitialPosition();

        // Define simulation counts to test
        int[] simulationCounts = { 1, 10, 50, 100 };
        int samplesPerCount = 10;  // Number of times to run each simulation count

        foreach (int simCount in simulationCounts)
        {
            Console.WriteLine($"\nTesting with {simCount} simulations:");
            Console.WriteLine("--------------------------------");

            var moves = new List<Move>();
            var visitCounts = new List<int>();
            var winRatios = new List<float>();
            var times = new List<double>();

            for (int sample = 0; sample < samplesPerCount; sample++)
            {
                var sw = Stopwatch.StartNew();
                var mcts = new AmoeballMCTS(state);
                mcts.RunSimulations(simCount);
                var bestMove = mcts.GetBestMove(randomizeSymmetry: false);
                sw.Stop();

                // Get statistics for the chosen move
                var stats = mcts.GetMoveStatistics(randomizeSymmetry: false)
                    .First(s => MovesAreEquivalent(s.move, bestMove));

                moves.Add(bestMove);
                visitCounts.Add(stats.visits);
                winRatios.Add(stats.winRatio);
                times.Add(sw.ElapsedMilliseconds);

                // Print progress
                Console.Write(".");
                if ((sample + 1) % 10 == 0) Console.WriteLine();
            }

            Console.WriteLine("\nResults:");

            // Analyze move consistency
            var moveGroups = moves.GroupBy(m => GetMoveKey(m))
                .OrderByDescending(g => g.Count());

            Console.WriteLine($"Move distribution across {samplesPerCount} samples:");
            foreach (var group in moveGroups)
            {
                var percentage = (double)group.Count() / samplesPerCount * 100;
                Console.WriteLine($"  Move {FormatMove(group.First())}: {group.Count()} times ({percentage:F1}%)");
            }

            // Print statistics
            Console.WriteLine($"Average visit count: {visitCounts.Average():F1}");
            Console.WriteLine($"Visit count std dev: {StdDev(visitCounts):F1}");
            Console.WriteLine($"Average win ratio: {winRatios.Average():F3}");
            Console.WriteLine($"Win ratio std dev: {StdDev(winRatios):F3}");
            Console.WriteLine($"Average time: {times.Average():F1}ms");
            Console.WriteLine($"Time std dev: {StdDev(times):F1}ms");
        }
    }

    private static bool MovesAreEquivalent(Move a, Move b)
    {
        if (!a.Position.Equals(b.Position)) return false;
        if (a.KickTarget.HasValue != b.KickTarget.HasValue) return false;
        if (a.KickTarget.HasValue && b.KickTarget.HasValue)
        {
            return a.KickTarget.Value.Equals(b.KickTarget.Value);
        }
        return true;
    }

    private static string GetMoveKey(Move move)
    {
        return move.KickTarget.HasValue
            ? $"{move.Position}=>{move.KickTarget.Value}"
            : move.Position.ToString();
    }

    private static string FormatMove(Move move)
    {
        return move.KickTarget.HasValue
            ? $"Place at {move.Position}, kick to {move.KickTarget.Value}"
            : $"Place at {move.Position}";
    }

    private static double StdDev<T>(IEnumerable<T> values) where T : IConvertible
    {
        var doubles = values.Select(x => Convert.ToDouble(x)).ToList();
        double avg = doubles.Average();
        return Math.Sqrt(doubles.Average(v => Math.Pow(v - avg, 2)));
    }
}