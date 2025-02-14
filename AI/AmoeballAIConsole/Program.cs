using Godot;
using System.Diagnostics;
using AmoeballAI;

public class Program
{
    public static void RunMCTS()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(120));
        Stopwatch stopwatch = Stopwatch.StartNew();

        var initialState = new AmoeballState();
        initialState.SetupInitialPosition();
        var tree = new OrderedGameTree(initialState);

        int totalSimulations = 0;

        while (!cts.IsCancellationRequested)
        {
            AmoeballMCTS.RunSimulations(tree, 1000, 9, cts.Token);
            totalSimulations += 1000;
            Console.WriteLine("Simulations Completed: {0}", totalSimulations);
            Console.WriteLine("Elapsed Time: {0} minutes", stopwatch.Elapsed.TotalMinutes);
            Console.WriteLine("Current Best Move: {0}", AmoeballMCTS.GetBestMove(tree, initialState).Position);
            Console.WriteLine();
        }

        tree.SaveToFile("MCTSResults.dat");
    }

    public static void Main()
    {
        var mcts = new MCTSPlayer(TimeSpan.FromSeconds(3),9, false);
        var mcts2 = new MCTSPlayer(TimeSpan.FromSeconds(5), 9, false);
        var random = new RandomPlayer();
        var random2 = new RandomPlayer();
        var oneply = new OnePlyPlayer();
        var oneply2 = new OnePlyPlayer();
        var runner = new GameRunner(mcts, oneply, verbose: true);
        runner.RunGames(1000);
    }

    public static void ProcessResults()
    {
        OrderedGameTree tree = OrderedGameTree.LoadFromFile("MCTSResults.dat");
        using var stream = new FileStream("MCTSResults.csv", FileMode.Create);
        using var writer = new StreamWriter(stream);
        writer.WriteLine("Hex Representation, Visits, Green Win Rate");

        for (int i = 0; i < tree.GetNodeCount(); i++)
        {
            if (tree.GetDepth(i) == 8)
            {
                writer.WriteLine("{0}, {1}, {2}", tree.GetState(i).Serialize().HexEncode(), tree.GetVisits(i), tree.GetWinRatio(i, AmoeballState.PieceType.GreenAmoeba));
            }
            
        }
    }


}