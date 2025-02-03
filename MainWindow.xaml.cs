using SnakeWPF.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;

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
            LoadLeaderboard();
            Style = (Style)FindResource(typeof(Window));
            volumeSlider.Value = 0.1;
            cbDifficulty.ItemsSource = Enum.GetValues(typeof(Difficulty)).Cast<Difficulty>();
            cbDifficulty.SelectedIndex = 0;
        }

        private SolidColorBrush _snakeBodyColor = Brushes.Blue;
        private SolidColorBrush _snakeHeadColor = Brushes.DarkBlue;
        private List<SnakePart> _snakeParts = new List<SnakePart>();

        const int SnakeSquareSize = 20;
        const int SnakeStartLength = 3;
        private int _snakeStartSpeed = 1000;
        const int SnakeSpeedThreshold = 50;
        public enum SnakeDirection { Left, Right, Up, Down };
        private SnakeDirection _snakeDirection = SnakeDirection.Right;
        private int _snakeLength;
        private int _currentScore = 0;

        private DispatcherTimer _gameTickTimer = new DispatcherTimer();

        private Random _rnd = new Random();

        private UIElement _snakeFood = null;
        private SolidColorBrush _foodColor = Brushes.Red;

        const int MaxHighscoreListEntryCount = 10;
        public ObservableCollection<SnakeHighscore> Highscores { get; set; } = new ObservableCollection<SnakeHighscore>();

        private enum Difficulty { Easy, Medium, Hard }
        private Difficulty _currentDifficulty = Difficulty.Easy;

        private List<UIElement> _obstacles = new List<UIElement>();
        private SolidColorBrush _obstacleColor = Brushes.Brown;

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
                case Key.Escape:
                    EndGame();
                    break;
            }
            if (_snakeDirection != originalSnakeDirection && _gameTickTimer.IsEnabled)
                MoveSnake();
        }

        #endregion

        #region Game

        private void BtnStartNewGame_Click(object sender, RoutedEventArgs e)
        {
            bdrMenu.Visibility = Visibility.Collapsed;
            bdrLeaderboard.Visibility = Visibility.Collapsed;
            bdrEndOfGame.Visibility = Visibility.Collapsed;

            SnakeScore.Visibility = Visibility.Visible;
            SnakeSpeed.Visibility = Visibility.Visible;

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

            // Set the difficulty
            switch (_currentDifficulty)
            {
                case Difficulty.Easy:
                    _snakeStartSpeed = 1000;
                    break;
                case Difficulty.Medium:
                    _snakeStartSpeed = 500;
                    break;
                case Difficulty.Hard:
                    _snakeStartSpeed = 100;
                    break;
                default:
                    break;
            }

            // Reset game variables
            _currentScore = 0;
            _snakeLength = SnakeStartLength;
            _snakeDirection = SnakeDirection.Right;
            _snakeParts.Add(new SnakePart() { Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5) });
            _gameTickTimer.Interval = TimeSpan.FromMilliseconds(_snakeStartSpeed);

            // Draw the snake again & the first food
            DrawSnake();
            DrawSnakeFood();
            DrawObstacle(10);

            // Update the games status to the default
            UpdateGameStatus();


            // Enable the timer to start off the game  
            _gameTickTimer.IsEnabled = true;
        }

        private void UpdateGameStatus()
        {
            this.tbStatusScore.Text = _currentScore.ToString();
            this.tbStatusSpeed.Text = _gameTickTimer.Interval.TotalMilliseconds.ToString();
        }

        private void EndGame()
        {
            bool isNewHighscore = false;
            PlayAudio("snakeGameOver.mp3");
            if (_currentScore > 0)
            {
                int lowestHighscore = (this.Highscores.Count > 0 ? this.Highscores.Min(x => x.Score) : 0);
                if ((_currentScore > lowestHighscore) || (this.Highscores.Count < MaxHighscoreListEntryCount))
                {
                    bdrNewHighscore.Visibility = Visibility.Visible;
                    txtPlayerName.Focus();
                    isNewHighscore = true;
                }
            }
            if (!isNewHighscore)
            {
                tbFinalScore.Text = _currentScore.ToString();
                bdrEndOfGame.Visibility = Visibility.Visible;
            }
            _gameTickTimer.IsEnabled = false;
        }

        #endregion

        #region Objects

        private Point GetNextObjectPosition()
        {
            int maxX = (int)(GameArea.ActualWidth / SnakeSquareSize);
            int maxY = (int)(GameArea.ActualHeight / SnakeSquareSize);

            Point newPosition;
            bool isPositionOccupied;

            do
            {
                int objectX = _rnd.Next(0, maxX) * SnakeSquareSize;
                int objectY = _rnd.Next(0, maxY) * SnakeSquareSize;
                newPosition = new Point(objectX, objectY);

                // Check if this position is occupied by the snake, food, or an obstacle
                isPositionOccupied = _snakeParts.Any(s => s.Position == newPosition) ||
                                     (_snakeFood != null &&
                                      Canvas.GetLeft(_snakeFood) == newPosition.X &&
                                      Canvas.GetTop(_snakeFood) == newPosition.Y) ||
                                     _obstacles.Any(o => Canvas.GetLeft(o) == newPosition.X &&
                                                         Canvas.GetTop(o) == newPosition.Y);

            } while (isPositionOccupied); // Repeat until we find an unoccupied position

            return newPosition;
        }

        private void DrawSnakeFood()
        {
            Point foodPosition = GetNextObjectPosition();
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

        private void DrawObstacle(int obstacleAmount)
        {
            for (int i = 0; i < obstacleAmount; i++)
            {
                Point obstaclePosition = GetNextObjectPosition();
                UIElement obstacle = new Rectangle()
                {
                    Width = SnakeSquareSize,
                    Height = SnakeSquareSize,
                    Fill = _obstacleColor
                };
                _obstacles.Add(obstacle);
                GameArea.Children.Add(obstacle);
                Canvas.SetTop(obstacle, obstaclePosition.Y);
                Canvas.SetLeft(obstacle, obstaclePosition.X);
            }
        }

        private void EatSnakeFood()
        {
            _snakeLength++;
            _currentScore++;
            int timerInterval = Math.Max(SnakeSpeedThreshold, (int)_gameTickTimer.Interval.TotalMilliseconds - (_currentScore * 2));
            _gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            GameArea.Children.Remove(_snakeFood);
            PlayAudio("snakeEat.mp3");
            DrawSnakeFood();
            UpdateGameStatus();
        }

        #endregion

        #region Collision Detection

        private void DoCollisionCheck()
        {
            SnakePart snakeHead = _snakeParts[_snakeParts.Count - 1];

            // Check if the snake eats the food
            if ((snakeHead.Position.X == Canvas.GetLeft(_snakeFood)) && (snakeHead.Position.Y == Canvas.GetTop(_snakeFood)))
            {
                EatSnakeFood();
                return;
            }

            // Check if the snake hits the walls
            if ((snakeHead.Position.Y < 0) || (snakeHead.Position.Y >= GameArea.ActualHeight) ||
                (snakeHead.Position.X < 0) || (snakeHead.Position.X >= GameArea.ActualWidth))
            {
                EndGame();
            }

            // Check if the snake hits itself
            foreach (SnakePart snakeBodyPart in _snakeParts.Take(_snakeParts.Count - 1))
            {
                if ((snakeHead.Position.X == snakeBodyPart.Position.X) && (snakeHead.Position.Y == snakeBodyPart.Position.Y))
                {
                    EndGame();
                    return;
                }
            }

            // Check if the snake hits an obstacle
            foreach (UIElement obstacle in _obstacles)
            {
                if ((snakeHead.Position.X == Canvas.GetLeft(obstacle)) && (snakeHead.Position.Y == Canvas.GetTop(obstacle)))
                {
                    EndGame();
                    return;
                }
            }

        }

        #endregion

        #region Window

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Leaderboard

        private void BtnShowLeaderboard_Click(object sender, RoutedEventArgs e)
        {
            bdrMenu.Visibility = Visibility.Collapsed;
            bdrLeaderboard.Visibility = Visibility.Visible;
        }

        private void LoadLeaderboard()
        {
            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); // Gets the path to the current user directory

            if (File.Exists($@"{userPath}\source\repos\SnakeWPF\snake_leaderboard.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<SnakeHighscore>));
                using (Stream reader = new FileStream($@"{userPath}\source\repos\SnakeWPF\snake_leaderboard.xml", FileMode.Open))
                {
                    List<SnakeHighscore> tempList = (List<SnakeHighscore>)serializer.Deserialize(reader);
                    this.Highscores.Clear();
                    foreach (var item in tempList.OrderByDescending(x => x.Score))
                        this.Highscores.Add(item);
                }
            }
        }

        private void SaveLeaderboard()
        {
            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); // Gets the path to the current user directory

            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<SnakeHighscore>));
            using (Stream writer = new FileStream($@"{userPath}\source\repos\SnakeWPF\snake_leaderboard.xml", FileMode.Create))
            {
                serializer.Serialize(writer, this.Highscores);
            }
        }

        private void BtnAddToLeaderboard_Click(object sender, RoutedEventArgs e)
        {
            int newIndex = 0;

            // Where should the new entry be inserted?
            if ((this.Highscores.Count > 0) && (_currentScore < this.Highscores.Max(x => x.Score)))
            {
                SnakeHighscore justAbove = this.Highscores.OrderByDescending(x => x.Score).First(x => x.Score >= _currentScore);
                if (justAbove != null)
                    newIndex = this.Highscores.IndexOf(justAbove) + 1;
            }

            // Create & insert the new entry
            this.Highscores.Insert(newIndex, new SnakeHighscore()
            {
                PlayerName = txtPlayerName.Text,
                Score = _currentScore
            });
            // Make sure that the amount of entries does not exceed the maximum
            while (this.Highscores.Count > MaxHighscoreListEntryCount)
                this.Highscores.RemoveAt(MaxHighscoreListEntryCount);

            SaveLeaderboard();

            txtPlayerName.Text = "";

            bdrNewHighscore.Visibility = Visibility.Collapsed;
            bdrLeaderboard.Visibility = Visibility.Visible;
        }

        #endregion

        #region Menu

        private void ButtonBackToMenu_Click(object sender, RoutedEventArgs e)
        {
            bdrMenu.Visibility = Visibility.Visible;
            bdrLeaderboard.Visibility = Visibility.Collapsed;
            bdrEndOfGame.Visibility = Visibility.Collapsed;
            SnakeScore.Visibility = Visibility.Collapsed;
            SnakeSpeed.Visibility = Visibility.Collapsed;
            bdrInstructions.Visibility = Visibility.Collapsed;
            bdrSettings.Visibility = Visibility.Collapsed;
        }

        private void ButtonShowInstructions_Click(object sender, RoutedEventArgs e)
        {
            bdrMenu.Visibility = Visibility.Collapsed;
            bdrInstructions.Visibility = Visibility.Visible;
        }
        private void btnShowSettings_Click(object sender, RoutedEventArgs e)
        {
            bdrMenu.Visibility = Visibility.Collapsed;
            bdrSettings.Visibility = Visibility.Visible;
        }

        #endregion

        #region Settings

        private void btnClearLeaderboard_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Would you like to clear the Leaderboard? This can not be undone.", "Clear Leaderboard", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    Highscores.Clear();
                    SaveLeaderboard();
                    break;
                case MessageBoxResult.No:
                case MessageBoxResult.Cancel:
                    MessageBox.Show("Ok, f*ck off then!", "F*ck Off", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    break;
                default:
                    break;
            }
        }
        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaElement.Volume = volumeSlider.Value;
            volumeValue.Text = Math.Floor(mediaElement.Volume * 100).ToString() + "%";
        }

        #endregion

        #region Audio

        private void PlayAudio(string file)
        {
            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); // Gets the path to the current user directory
            string filePath = userPath + @"\source\repos\SnakeWPF\Assets\Sounds\" + file;
            mediaElement.Source = new Uri(filePath);
            mediaElement.Play();
        }

        #endregion

        #region Difficulty

        private void cbDifficulty_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbDifficulty.SelectedItem)
            {
                case Difficulty.Easy:
                    _currentDifficulty = Difficulty.Easy;
                    break;
                case Difficulty.Medium:
                    _currentDifficulty = Difficulty.Medium;
                    break;
                case Difficulty.Hard:
                    _currentDifficulty = Difficulty.Hard;
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}