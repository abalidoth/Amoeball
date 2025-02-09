using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Program
{
    public static void Main()
    {
        MCTSBenchmark.RunTest();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(45));
        Stopwatch stopwatch = Stopwatch.StartNew();

        var initialState = new AmoeballState();
        initialState.SetupInitialPosition();
        var mcts = new AmoeballMCTS(initialState, 8);

        int totalSimulations = 0;

        while (!cts.IsCancellationRequested)
        {
            mcts.RunSimulations(100, cts.Token);
            totalSimulations += 100;
            Console.WriteLine("Simulations Completed: {0}", totalSimulations);
            Console.WriteLine("Elapsed Time: {0} minutes", stopwatch.Elapsed.TotalMinutes);
        }

        mcts.SaveToFile("MCTSResults.dat");
    }
}