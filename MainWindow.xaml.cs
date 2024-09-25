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
        }

        const int _snakeSquareSize = 20;
        private SolidColorBrush _snakeBodyBrush = Brushes.Blue;
        private SolidColorBrush _snakeHeadBrush = Brushes.DarkBlue;
        private List<SnakePart> _snakeParts = new List<SnakePart>();

        public enum SnakeDirection { Left, Right, Up, Down };
        private SnakeDirection _snakeDirection = SnakeDirection.Right;
        private int _snakeLength;

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();
        }
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
                    Width = _snakeSquareSize,
                    Height = _snakeSquareSize,
                    Fill = isPrimaryColor ? Brushes.Green : Brushes.DarkGreen
                };

                GameArea.Children.Add(rectangle);
                Canvas.SetTop(rectangle, nextY);
                Canvas.SetLeft(rectangle, nextX);

                isPrimaryColor = !isPrimaryColor;
                nextX += _snakeSquareSize;

                if(nextX >= GameArea.ActualWidth)
                {
                    nextX = 0;
                    nextY += _snakeSquareSize;
                    rowCounter++;
                    isPrimaryColor = rowCounter % 2 != 0;
                }

                if(nextY >= GameArea.ActualHeight)
                {
                    doneDrawingGameArea = true;
                }
            }
        }

        private void DrawSnake()
        {
            foreach (SnakePart snakePart in _snakeParts)
            {
                if (snakePart.UiElement == null)
                {
                    snakePart.UiElement = new Rectangle()
                    {
                        Width = _snakeSquareSize,
                        Height = _snakeSquareSize,
                        Fill = snakePart.IsHead ? _snakeHeadBrush : _snakeBodyBrush
                    };
                    GameArea.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
                }
            }
        }

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
                (snakePart.UiElement as Rectangle).Fill = _snakeBodyBrush;
                snakePart.IsHead = false;
            }

            // Determine in which direction to expand the snake, based on the snakes current direction  
            SnakePart snakeHead = _snakeParts[_snakeParts.Count - 1];
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;
            switch (_snakeDirection)
            {
                case SnakeDirection.Left:
                    nextX -= _snakeSquareSize;
                    break;
                case SnakeDirection.Right:
                    nextX += _snakeSquareSize;
                    break;
                case SnakeDirection.Up:
                    nextY -= _snakeSquareSize;
                    break;
                case SnakeDirection.Down:
                    nextY += _snakeSquareSize;
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
    }
}