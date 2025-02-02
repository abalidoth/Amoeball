using System;
using System.Collections.Generic;
using System.Diagnostics;
using static AmoeballState;

public class GameTreeBenchmark
{
    private readonly OrderedGameTree _tree;
    private readonly Stopwatch _stopwatch;

    public GameTreeBenchmark()
    {
        var initialState = new AmoeballState();
        initialState.SetupInitialPosition();
        _tree = new OrderedGameTree(initialState, 1000000);
        _stopwatch = new Stopwatch();
    }

    public void RunBenchmark(int maxDepth)
    {
        Console.WriteLine("Starting Game Tree Expansion Benchmark");
        Console.WriteLine("=====================================");
        Console.WriteLine($"{"Depth",-6} {"Nodes",-10} {"New Nodes",-10} {"Time (ms)",-10} {"Cumulative Time (ms)",-20}");
        Console.WriteLine("----------------------------------------------");

        long totalTime = 0;
        int previousNodeCount = 0;
        int[] nodesByDepth;

        var initialState = new AmoeballState();
        initialState.SetupInitialPosition();
        

        // Start with root node at depth 0
        var rootIndex = _tree.FindStateIndex(initialState);

        for (int depth = 0; depth <= maxDepth; depth++)
        {
            _stopwatch.Restart();


            // Expand all nodes at current depth
            nodesByDepth = _tree.GetNodesAtDepth(depth);
            foreach (var nodeIndex in nodesByDepth)
            {
                _tree.Expand(nodeIndex);

            }
            _stopwatch.Stop();
            totalTime += _stopwatch.ElapsedMilliseconds;

            int currentNodeCount = _tree.GetNodeCount();
            int newNodes = currentNodeCount - previousNodeCount;

            Console.WriteLine($"{depth,-6} {currentNodeCount,-10} {newNodes,-10} {_stopwatch.ElapsedMilliseconds,-10} {totalTime,-20}");

            previousNodeCount = currentNodeCount;

            // Early exit if no new nodes were added
            if (newNodes == 0 && depth < maxDepth)
            {
                Console.WriteLine($"\nNo new nodes added at depth {depth}. Tree expansion complete.");
                break;
            }
        }

        Console.WriteLine("\nAdditional Statistics:");
        Console.WriteLine($"Average probe length: {_tree.GetAverageProbeLength():F2}");
    }

    public static void Main()
    {
        var benchmark = new GameTreeBenchmark();
        benchmark.RunBenchmark(7);
    }
}