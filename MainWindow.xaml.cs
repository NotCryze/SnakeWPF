using SnakeWPF.Models;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static SnakeWPF.MainWindow;

namespace SnakeWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            _gameTickTimer.Tick += GameTickTimer_Tick;
        }

        private SolidColorBrush _snakeBodyColor = Brushes.Blue;
        private SolidColorBrush _snakeHeadColor = Brushes.DarkBlue;
        private List<SnakePart> _snakeParts = new List<SnakePart>();

        const int SnakeSquareSize = 20;
        const int SnakeStartLength = 3;
        const int SnakeStartSpeed = 400;
        const int SnakeSpeedThreshold = 100;
        public enum SnakeDirection { Left, Right, Up, Down };
        private SnakeDirection _snakeDirection = SnakeDirection.Right;
        private int _snakeLength;
        private int _currentScore = 0;

        private DispatcherTimer _gameTickTimer = new DispatcherTimer();

        private Random _rnd = new Random();

        private UIElement _snakeFood = null;
        private SolidColorBrush _foodColor = Brushes.Red;

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();
        }

        #region Game Area

        private void DrawGameArea()
        {
            bool doneDrawingGameArea = false;
            int nextX = 0, nextY = 0;
            int rowCounter = 0;
            bool isPrimaryColor = false;

            while (!doneDrawingGameArea)
            {
                Rectangle rectangle = new Rectangle
                {
                    Width = SnakeSquareSize,
                    Height = SnakeSquareSize,
                    Fill = isPrimaryColor ? Brushes.Green : Brushes.DarkGreen
                };

                GameArea.Children.Add(rectangle);
                Canvas.SetTop(rectangle, nextY);
                Canvas.SetLeft(rectangle, nextX);

                isPrimaryColor = !isPrimaryColor;
                nextX += SnakeSquareSize;

                if (nextX >= GameArea.ActualWidth)
                {
                    nextX = 0;
                    nextY += SnakeSquareSize;
                    rowCounter++;
                    isPrimaryColor = rowCounter % 2 != 0;
                }

                if (nextY >= GameArea.ActualHeight)
                {
                    doneDrawingGameArea = true;
                }
            }
        }

        #endregion

        #region Snake Body

        private void DrawSnake()
        {
            foreach (SnakePart snakePart in _snakeParts)
            {
                if (snakePart.UiElement == null)
                {
                    snakePart.UiElement = new Rectangle()
                    {
                        Width = SnakeSquareSize,
                        Height = SnakeSquareSize,
                        Fill = snakePart.IsHead ? _snakeHeadColor : _snakeBodyColor
                    };
                    GameArea.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
                }
            }
        }

        #endregion

        #region Snake Movement

        private void MoveSnake()
        {
            // Remove the last part of the snake, in preparation of the new part 
            while (_snakeParts.Count >= _snakeLength)
            {
                GameArea.Children.Remove(_snakeParts[0].UiElement);
                _snakeParts.RemoveAt(0);
            }

            // Mark all snake parts as non-head and change their color to the body color to prepare for the new head
            foreach (SnakePart snakePart in _snakeParts)
            {
                (snakePart.UiElement as Rectangle).Fill = _snakeBodyColor;
                snakePart.IsHead = false;
            }

            // Determine in which direction to expand the snake, based on the snakes current direction  
            SnakePart snakeHead = _snakeParts[_snakeParts.Count - 1];
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;
            switch (_snakeDirection)
            {
                case SnakeDirection.Left:
                    nextX -= SnakeSquareSize;
                    break;
                case SnakeDirection.Right:
                    nextX += SnakeSquareSize;
                    break;
                case SnakeDirection.Up:
                    nextY -= SnakeSquareSize;
                    break;
                case SnakeDirection.Down:
                    nextY += SnakeSquareSize;
                    break;
            }

            // Add the new head part 
            _snakeParts.Add(new SnakePart()
            {
                Position = new Point(nextX, nextY),
                IsHead = true
            });

            // Draws the snake after it has been moved
            DrawSnake();

            // Checks if the snake collided with anything (Food, Wall or itself)
            DoCollisionCheck();
        }

        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnake();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            SnakeDirection originalSnakeDirection = _snakeDirection;
            switch (e.Key)
            {
                case Key.Up:
                case Key.W:
                    if (_snakeDirection != SnakeDirection.Down)
                        _snakeDirection = SnakeDirection.Up;
                    break;
                case Key.Down:
                case Key.S:
                    if (_snakeDirection != SnakeDirection.Up)
                        _snakeDirection = SnakeDirection.Down;
                    break;
                case Key.Left:
                case Key.A:
                    if (_snakeDirection != SnakeDirection.Right)
                        _snakeDirection = SnakeDirection.Left;
                    break;
                case Key.Right:
                case Key.D:
                    if (_snakeDirection != SnakeDirection.Left)
                        _snakeDirection = SnakeDirection.Right;
                    break;
                case Key.Space:
                    StartNewGame();
                    break;
            }
            if (_snakeDirection != originalSnakeDirection && _gameTickTimer.IsEnabled)
                MoveSnake();
        }

        #endregion

        #region Game

        private void StartNewGame()
        {
            // Remove potential dead snake parts
            foreach (SnakePart snakeBodyPart in _snakeParts)
            {
                if (snakeBodyPart.UiElement != null)
                    GameArea.Children.Remove(snakeBodyPart.UiElement);
            }
            _snakeParts.Clear();

            // Remove any remaining food
            if (_snakeFood != null)
                GameArea.Children.Remove(_snakeFood);

            // Reset game variables
            _currentScore = 0;
            _snakeLength = SnakeStartLength;
            _snakeDirection = SnakeDirection.Right;
            _snakeParts.Add(new SnakePart() { Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5) });
            _gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);

            // Draw the snake again & the first food
            DrawSnake();
            DrawSnakeFood();

            // Update the games status to the default
            UpdateGameStatus();

            // Enable the timer to start off the game  
            _gameTickTimer.IsEnabled = true;
        }

        private void UpdateGameStatus()
        {
            this.Title = "Snake - Score: " + _currentScore + " - Game speed: " + _gameTickTimer.Interval.TotalMilliseconds;
        }

        private void EndGame()
        {
            _gameTickTimer.IsEnabled = false;
            MessageBox.Show("Oooops, you died!\n\nTo start a new game, just press the Space bar...", "SnakeWPF");
        }

        #endregion

        #region Food

        private Point GetNextFoodPosition()
        {
            int maxX = (int)(GameArea.ActualWidth / SnakeSquareSize);
            int maxY = (int)(GameArea.ActualHeight / SnakeSquareSize);
            int foodX = _rnd.Next(0, maxX) * SnakeSquareSize;
            int foodY = _rnd.Next(0, maxY) * SnakeSquareSize;

            foreach (SnakePart snakePart in _snakeParts)
            {
                if ((snakePart.Position.X == foodX) && (snakePart.Position.Y == foodY))
                    return GetNextFoodPosition();
            }

            return new Point(foodX, foodY);
        }

        private void DrawSnakeFood()
        {
            Point foodPosition = GetNextFoodPosition();
            _snakeFood = new Ellipse()
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                Fill = _foodColor
            };
            GameArea.Children.Add(_snakeFood);
            Canvas.SetTop(_snakeFood, foodPosition.Y);
            Canvas.SetLeft(_snakeFood, foodPosition.X);
        }

        private void EatSnakeFood()
        {
            _snakeLength++;
            _currentScore++;
            int timerInterval = Math.Max(SnakeSpeedThreshold, (int)_gameTickTimer.Interval.TotalMilliseconds - (_currentScore * 2));
            _gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            GameArea.Children.Remove(_snakeFood);
            DrawSnakeFood();
            UpdateGameStatus();
        }

        #endregion

        #region Collision Detection

        private void DoCollisionCheck()
        {
            SnakePart snakeHead = _snakeParts[_snakeParts.Count - 1];

            if ((snakeHead.Position.X == Canvas.GetLeft(_snakeFood)) && (snakeHead.Position.Y == Canvas.GetTop(_snakeFood)))
            {
                EatSnakeFood();
                return;
            }

            if ((snakeHead.Position.Y < 0) || (snakeHead.Position.Y >= GameArea.ActualHeight) ||
            (snakeHead.Position.X < 0) || (snakeHead.Position.X >= GameArea.ActualWidth))
            {
                EndGame();
            }

            foreach (SnakePart snakeBodyPart in _snakeParts.Take(_snakeParts.Count - 1))
            {
                if ((snakeHead.Position.X == snakeBodyPart.Position.X) && (snakeHead.Position.Y == snakeBodyPart.Position.Y))
                    EndGame();
            }
        }

        #endregion

        #region Window

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        #endregion
    }
}