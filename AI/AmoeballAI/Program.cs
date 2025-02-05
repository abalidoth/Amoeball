using System;
using System.Collections.Generic;
using System.Diagnostics;
using static AmoeballState;

public class MCTSConvergenceTest
{
    // Checkpoints where we'll record statistics
    private static readonly int[] CHECKPOINTS = { 100, 200, 500, 1000, 2000, 5000, 10000 };

    public static void RunConvergenceTest()
    {
        var state = new AmoeballState();
        state.SetupInitialPosition();

        Console.WriteLine("Starting MCTS convergence test from initial position...\n");

        // Track stats for each move at each checkpoint
        var moveStats = new Dictionary<string, List<(int sims, int visits, double winRate)>>();

        foreach (int numSims in CHECKPOINTS)
        {
            Console.WriteLine($"\nRunning {numSims} simulations...");

            var mcts = new AmoeballMCTS(state);
            var move = mcts.FindBestMove(numSims);

            // Get statistics for all possible moves
            var stats = mcts.GetMoveStatistics().ToList();

            // Print detailed stats for this checkpoint
            Console.WriteLine("\nMove statistics:");
            Console.WriteLine($"{"Position",-12} {"Kick Target",-12} {"Visits",-8} {"Visit %",-8} {"Win %",-8}");
            Console.WriteLine(new string('-', 50));

            foreach (var (mv, visits, winRate) in stats)
            {
                string moveKey = $"{mv.Position.X},{mv.Position.Y}";
                string kickTarget = mv.KickTarget.HasValue ?
                    $"({mv.KickTarget.Value.X},{mv.KickTarget.Value.Y})" : "None";

                // Store stats for tracking convergence
                if (!moveStats.ContainsKey(moveKey))
                {
                    moveStats[moveKey] = new List<(int, int, double)>();
                }
                moveStats[moveKey].Add((numSims, visits, winRate));

                // Print current stats
                Console.WriteLine(
                    $"({mv.Position.X},{mv.Position.Y}){"",-4} " +
                    $"{kickTarget,-12} " +
                    $"{visits,-8} " +
                    $"{((double)visits / numSims):P1} " +
                    $"{winRate:P1}");
            }
        }

        // Print convergence summary for each move
        Console.WriteLine("\nConvergence Summary:");
        foreach (var (moveKey, stats) in moveStats)
        {
            Console.WriteLine($"\nMove {moveKey}:");
            Console.WriteLine($"{"Sims",-8} {"Visits",-8} {"Visit %",-8} {"Win %",-8}");
            Console.WriteLine(new string('-', 40));

            foreach (var (sims, visits, winRate) in stats)
            {
                Console.WriteLine(
                    $"{sims,-8} " +
                    $"{visits,-8} " +
                    $"{((double)visits / sims):P1} " +
                    $"{winRate:P1}");
            }
        }
    }

    public static void Main()
    {
        RunConvergenceTest();
    }
}