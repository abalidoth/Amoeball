using Godot;
using System.Diagnostics;
using static AmoeballAI.AmoeballState;

namespace AmoeballAI
{

    public class GameTreeBenchmark
    {
        private readonly OrderedGameTree _tree;
        private readonly Dictionary<int, int> _nodesPerDepth;
        private readonly Dictionary<int, long> _timePerDepth;
        private readonly Dictionary<int, long> _memoryPerDepth;
        private readonly Stopwatch _stopwatch;
        private int _currentDepth = -1;

        public GameTreeBenchmark()
        {
            var initialState = new AmoeballState();
            initialState.SetupInitialPosition();
            _tree = new OrderedGameTree(initialState);
            _nodesPerDepth = new Dictionary<int, int>();
            _timePerDepth = new Dictionary<int, long>();
            _memoryPerDepth = new Dictionary<int, long>();
            _stopwatch = new Stopwatch();

            // Print header
            Console.WriteLine("\nGame Tree Analysis");
            Console.WriteLine("=================");
            Console.WriteLine("Depth | Nodes | Branch Factor | Time (ms) | Memory (MB)");
            Console.WriteLine("------------------------------------------------");
        }

        private void PrintDepthStatistics(int depth)
        {
            int nodesAtDepth = _nodesPerDepth[depth];
            double branchingFactor = 0;
            if (depth > 0 && _nodesPerDepth.ContainsKey(depth - 1) && _nodesPerDepth[depth - 1] > 0)
            {
                branchingFactor = (double)nodesAtDepth / _nodesPerDepth[depth - 1];
            }

            long timeForDepth = _timePerDepth[depth];
            if (depth > 0)
            {
                timeForDepth -= _timePerDepth[depth - 1];
            }

            long memoryUsage = _memoryPerDepth[depth] / (1024 * 1024); // Convert to MB

            Console.WriteLine($"{depth,5} | {nodesAtDepth,6} | {branchingFactor,12:F2} | {timeForDepth,8} | {memoryUsage,10}");
        }

        private void UpdateMemoryUsage(int depth)
        {
            GC.Collect(); // Force garbage collection to get accurate reading
            _memoryPerDepth[depth] = GC.GetTotalMemory(true);
        }

        public void ExpandToDepth(int targetDepth)
        {
            Console.WriteLine($"\nStarting tree expansion to depth {targetDepth}...\n");
            _stopwatch.Start();

            var nodesToExpand = new Queue<int>();
            nodesToExpand.Enqueue(0); // Start with root node

            // Initialize depth 0 statistics
            _nodesPerDepth[0] = 1;
            _timePerDepth[0] = 0;
            UpdateMemoryUsage(0);
            PrintDepthStatistics(0);

            while (nodesToExpand.Count > 0)
            {
                int nodeIndex = nodesToExpand.Dequeue();
                var currentDepth = _tree.GetDepth(nodeIndex);

                if (currentDepth > targetDepth)
                    continue;

                // If we've moved to a new depth, print statistics for the previous depth
                if (currentDepth > _currentDepth)
                {
                    _currentDepth = currentDepth;
                    _timePerDepth[currentDepth] = _stopwatch.ElapsedMilliseconds;
                    UpdateMemoryUsage(currentDepth);

                    if (_nodesPerDepth.ContainsKey(currentDepth))
                    {
                        PrintDepthStatistics(currentDepth);
                    }
                }

                // Expand the node
                _tree.Expand(nodeIndex);

                // Get children and add them to the queue
                var children = _tree.GetChildIndices(nodeIndex);
                foreach (var childIndex in children)
                {
                    nodesToExpand.Enqueue(childIndex);
                    var childDepth = currentDepth + 1;
                    _nodesPerDepth[childDepth] = _nodesPerDepth.GetValueOrDefault(childDepth, 0) + 1;
                }
            }

            _stopwatch.Stop();

            // Print final summary
            Console.WriteLine("\nFinal Summary:");
            Console.WriteLine($"Total nodes: {_tree.GetNodeCount():N0}");
            Console.WriteLine($"Total time: {_stopwatch.ElapsedMilliseconds:N0}ms");
            Console.WriteLine($"Final memory usage: {_memoryPerDepth[_currentDepth] / (1024 * 1024):N0}MB");

            if (_nodesPerDepth.Count > 1)
            {
                double avgBranchingFactor = _nodesPerDepth
                    .Where(x => x.Key < _nodesPerDepth.Keys.Max())
                    .Average(x => _nodesPerDepth.GetValueOrDefault(x.Key + 1, 0) / (double)x.Value);
                Console.WriteLine($"Average branching factor: {avgBranchingFactor:F2}");
            }
        }

        public Dictionary<int, int> GetNodesPerDepth() => new Dictionary<int, int>(_nodesPerDepth);
        public Dictionary<int, long> GetTimePerDepth() => new Dictionary<int, long>(_timePerDepth);
        public Dictionary<int, long> GetMemoryPerDepth() => new Dictionary<int, long>(_memoryPerDepth);
        public int GetTotalNodes() => _tree.GetNodeCount();
        public long GetTotalTime() => _stopwatch.ElapsedMilliseconds;
    }
}