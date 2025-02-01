using System;
using System.Collections.Generic;
using System.Diagnostics;
using static AmoeballState;

public class GameTreeBenchmark
{
    private GameTreeSystem _tree;
    private Dictionary<int, long> _nodesPerDepth;
    private Stopwatch _stopwatch;

    public GameTreeBenchmark()
    {
        _tree = new GameTreeSystem(1000000); // Start with a larger initial capacity
        _nodesPerDepth = new Dictionary<int, long>();
        _stopwatch = new Stopwatch();
    }

    public void RunBenchmark(int maxDepth)
    {
        Console.WriteLine($"Starting benchmark to depth {maxDepth}...\n");

        // Initialize with starting position
        var initialState = new AmoeballState();
        initialState.SetupInitialPosition();

        _stopwatch.Restart();
        _tree.InitializeRoot(initialState);

        // Expand tree level by level
        ExpandToDepth(maxDepth);

        _stopwatch.Stop();
        PrintResults(maxDepth);
    }

    private void ExpandToDepth(int maxDepth)
    {
        for (int depth = 0; depth <= maxDepth; depth++)
        {
            long nodesAtDepth = 0;
            int currentTreeSize = _tree.GetNodeCount();

            // Get all nodes at current depth
            for (int nodeIndex = 0; nodeIndex <= currentTreeSize; nodeIndex++)
            {
                if (_tree.GetDepth(nodeIndex) == depth && !_tree.IsExpanded(nodeIndex))
                {
                    _tree.Expand(nodeIndex);
                    nodesAtDepth++;
                }
            }

            _nodesPerDepth[depth] = nodesAtDepth;
        }
    }

    private void PrintResults(int maxDepth)
    {
        Console.WriteLine("Benchmark Results:");
        Console.WriteLine("==================");
        Console.WriteLine($"Total time: {_stopwatch.ElapsedMilliseconds:N0}ms");
        Console.WriteLine($"Total states: {_tree.GetStateCount():N0}");
        Console.WriteLine($"Total nodes: {_tree.GetNodeCount():N0}");
        Console.WriteLine("\nNodes per depth:");

        long totalNodes = 0;
        for (int depth = 0; depth <= maxDepth; depth++)
        {
            if (_nodesPerDepth.TryGetValue(depth, out long nodes))
            {
                totalNodes += nodes;
                Console.WriteLine($"Depth {depth}: {nodes:N0} nodes ({totalNodes:N0} total)");
            }
        }

        double avgBranchingFactor = Math.Pow(_tree.GetNodeCount(), 1.0 / (maxDepth + 1));
        Console.WriteLine($"\nAverage branching factor: {avgBranchingFactor:F2}");
        unsafe
        {
            // Memory usage (approximate)
            long estimatedMemoryBytes = _tree.GetNodeCount() * (
                sizeof(GameStateComponent) +
                sizeof(TreeStructureComponent)
            )
            + _tree.GetStateCount() * (
                sizeof(StateStatistics) +
                sizeof(int) * 2 +
                sizeof(byte)* AmoeballState.GetSerializedSize(HexGrid.RADIUS)
                )
                ;
            ;


            Console.WriteLine($"Estimated memory usage: {estimatedMemoryBytes / 1024.0 / 1024.0:F2} MB");
        }
    }

    public static void Main()
    {
        var benchmark = new GameTreeBenchmark();

        // Run benchmarks for depths 0 through 3
        for (int depth = 1; depth <= 7; depth++)
        {
            Console.WriteLine($"\n=== Benchmark for Depth {depth} ===\n");
            benchmark = new GameTreeBenchmark(); // Fresh instance for each depth
            benchmark.RunBenchmark(depth);
        }
    }
}