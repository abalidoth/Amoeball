using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AmoeballAI;
using Godot;
using System.Diagnostics.CodeAnalysis;
using static AmoeballAI.AmoeballState;

namespace GameTreeExplorer
{
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        private readonly OrderedGameTree _tree;
        private AmoeballState _state;
        private const double HEX_SIZE = 30;
        private AmoeballState[] _childStates;
        private int[] _childIndices;

        // Observable properties
        private string _currentPlayerText;
        private string _turnStepText;
        private string _visitCountText;
        private string _winRatioText;
        private int _childCount;

        
        public string CurrentPlayerText
        {
            get => _currentPlayerText;
            [MemberNotNull(nameof(_currentPlayerText))]
            set { _currentPlayerText = value; OnPropertyChanged(); }
        }

        public string TurnStepText
        {
            get => _turnStepText;
            [MemberNotNull(nameof(_turnStepText))]
            set { _turnStepText = value; OnPropertyChanged(); }
        }

        public string VisitCountText
        {
            get => _visitCountText;
            [MemberNotNull(nameof(_visitCountText))]
            set { _visitCountText = value; OnPropertyChanged(); }
        }

        public string WinRatioText
        {
            get => _winRatioText;
            [MemberNotNull(nameof(_winRatioText))]
            set { _winRatioText = value; OnPropertyChanged(); }
        }

        public int ChildCount
        {
            get => _childCount;
            set { _childCount = value; OnPropertyChanged(); }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _tree = OrderedGameTree.LoadFromFile("MCTSResults.dat");
            _state = _tree.GetState(0);

            // Initial update
            UpdateDisplay();

            // Handle canvas size changes
            GameBoard.SizeChanged += (s, e) => DrawGameBoard();

            // Initial update will happen after layout is complete
            Loaded += (s, e) => DrawGameBoard();
        }

        [MemberNotNull(nameof(_currentPlayerText))]
        [MemberNotNull(nameof(_turnStepText))]
        [MemberNotNull(nameof(_visitCountText))]
        [MemberNotNull(nameof(_winRatioText))]
        [MemberNotNull(nameof(_childStates))]
        [MemberNotNull(nameof(_childIndices))]
        private void UpdateDisplay()
        {
            DrawGameBoard();
            UpdateNodeInfo();
            UpdateChildList();
        }

        private void DrawGameBoard()
        {
            GameBoard.Children.Clear();
            var grid = HexGrid.Instance;
            var centerX = GameBoard.ActualWidth / 2;
            var centerY = GameBoard.ActualHeight / 2;

            for (int i = 0; i < grid.TotalCells; i++)
            {
                var coord = grid.GetCoordinate(i);
                var piece = _state.GetPiece(coord);
                var (x, y) = AxialToPixel(coord, HEX_SIZE);
                DrawHex(centerX + x, centerY + y, HEX_SIZE, GetPieceColor(piece));
                
            }
        }

        private void DrawHex(double centerX, double centerY, double size, Brush fill)
        {
            var points = new PointCollection();
            for (int i = 0; i < 6; i++)
            {
                double angle = Math.PI / 6 + (Math.PI / 3 * i);
                points.Add(new System.Windows.Point(
                    centerX + size * Math.Cos(angle),
                    centerY + size * Math.Sin(angle)
                ));
            }

            var hex = new Polygon
            {
                Points = points,
                Fill = fill,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            GameBoard.Children.Add(hex);
        }

        private (double x, double y) AxialToPixel(Vector2I coord, double size)
        {
            var x = size * (Math.Sqrt(3) * coord.X + Math.Sqrt(3) / 2 * coord.Y);
            var y = size * (3.0 / 2 * coord.Y);
            return (x, y);
        }

        private Brush GetPieceColor(PieceType piece)
        {
            return piece switch
            {
                PieceType.GreenAmoeba => Brushes.Green,
                PieceType.PurpleAmoeba => Brushes.Purple,
                PieceType.Ball => Brushes.Yellow,
                _ => Brushes.LightGray
            };
        }

        [MemberNotNull(nameof(_currentPlayerText))]
        [MemberNotNull(nameof(_turnStepText))]
        [MemberNotNull(nameof(_visitCountText))]
        [MemberNotNull(nameof(_winRatioText))]
        private void UpdateNodeInfo()
        {
            var _currentNodeIndex = _tree.FindStateIndex(_state);
            CurrentPlayerText = $"{_state.CurrentPlayer} to move.";
            TurnStepText = $"Turn Step: {_state.TurnStep}";
            VisitCountText = $"Visit Count: {_tree.GetVisits(_currentNodeIndex)}";

            var greenWinRatio = _tree.GetWinRatio(_currentNodeIndex, PieceType.GreenAmoeba);
            var purpleWinRatio = _tree.GetWinRatio(_currentNodeIndex, PieceType.PurpleAmoeba);
            WinRatioText = $"Win Ratio - Green: {greenWinRatio:P2}, Purple: {purpleWinRatio:P2}";
        }

        [MemberNotNull(nameof(_childStates))]
        [MemberNotNull(nameof(_childIndices))]
        private void UpdateChildList()
        {
            var childStates = _state.GetNextStates();

            ChildList.Items.Clear();

            // Create sorted list of child indices based on visit count
            Dictionary<int,AmoeballState> distinctChildStates = new Dictionary<int,AmoeballState>();
            foreach (var childState in childStates)
            {
                var childIndex = _tree.FindStateIndex(childState);
                if (!distinctChildStates.ContainsKey(childIndex))
                {
                    distinctChildStates.Add(childIndex, childState);
                }
            }
            _childIndices = distinctChildStates.Keys.OrderByDescending(index => _tree.GetVisits(index)).ToArray();
            _childStates = _childIndices.Select(index => distinctChildStates[index]).ToArray();

            foreach (var index in _childIndices)
            {
                var childState = distinctChildStates[index];
                var visits = _tree.GetVisits(index);
                var greenWinRate = _tree.GetWinRatio(index, PieceType.GreenAmoeba);

                ChildList.Items.Add(
                    $"Node: {childState.LastMove.Position} | " +
                    $"Visits: {visits,6} | " +
                    $"Win% G: {greenWinRate:P1}"
                    
                );
            }

            ChildCount = _childIndices.Length;
            ChildList.SelectedIndex = -1;
        }

        private void ParentButton_Click(object sender, RoutedEventArgs e)
        {
            var _currentNodeIndex = _tree.FindStateIndex(_state);
            var parentIndex = _tree.GetParent(_currentNodeIndex);
            if (parentIndex != -1)
            {
                _state = _tree.GetState(parentIndex);
                UpdateDisplay();
            }
        }

        private void ChildList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIndex = ChildList.SelectedIndex;
            if (selectedIndex < 0) return;

            var currentNodeIndex = _tree.FindStateIndex(_state);
            _tree.SetParent(_childIndices[selectedIndex], currentNodeIndex);
            _state = _childStates[selectedIndex];
            UpdateDisplay();
    }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
