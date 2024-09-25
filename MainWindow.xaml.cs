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

        private DispatcherTimer _gameTickTimer = new DispatcherTimer();

        private Random _rnd = new Random();

        private UIElement _snakeFood = null;
        private SolidColorBrush _foodColor = Brushes.Red;

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();
            StartNewGame();
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
        }

        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnake();
        }

        #endregion

        #region Game Startup

        private void StartNewGame()
        {
            _snakeLength = SnakeStartLength;
            _snakeDirection = SnakeDirection.Right;
            _snakeParts.Add(new SnakePart() { Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5) });
            _gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);

            // Draw the snake & first food 
            DrawSnake();
            DrawSnakeFood();

            // Starts off the timer to make the snake move         
            _gameTickTimer.IsEnabled = true;
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

        #endregion
    }
}