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

namespace SnakeWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int _snakeSquareSize = 20;
        public MainWindow()
        {
            InitializeComponent();
        }

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
    }
}