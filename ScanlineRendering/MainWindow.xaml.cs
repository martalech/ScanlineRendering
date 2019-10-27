using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ScanlineRendering
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Point[,] vertices;
        private TrianglesInfo triangles;
        private (int i, int j)? movingPoint = null;

        public event PropertyChangedEventHandler PropertyChanged;
        public TrianglesInfo Triangles
        {
            get
            {
                return triangles;
            }
            set
            {
                if (value != triangles)
                {
                    triangles = value;
                    RaisePropertyChanged();
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Triangles = new TrianglesInfo("40", "50");
            Triangles.PropertyChanged += OnTrianglesPropertyChanged;
        }

        private void OnTrianglesPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RepaintTriangles(true);
        }

        public void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            var mouse = e.GetPosition(Board);
            for (int i = 0; i < vertices.GetLength(0); i++)
            {
                for (int j = 0; j < vertices.GetLength(1); j++)
                {
                    Point vertex = vertices[i, j];
                    if (BelongsToCircle(mouse, vertex))
                    {
                        movingPoint = (i, j);
                        return;
                    }
                }
            }
            movingPoint = null;
        }

        private void OnCanvasLoaded(object sender, RoutedEventArgs e)
        {
            RepaintTriangles(true);
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            var mouse = e.GetPosition(Board);
            if (e.LeftButton == MouseButtonState.Pressed && movingPoint != null)
            {
                var point = movingPoint.Value;
                vertices[point.i, point.j] = new Point(mouse.X, mouse.Y);
                RepaintTriangles();
            }
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            movingPoint = null;
        }

        private void OnCanvasMouseLeave(object sender, MouseEventArgs e)
        {
            movingPoint = null;
        }

        private void RepaintTriangles(bool clear = false)
        {
            Board.Children.Clear();
            int n, m;
            int.TryParse(Triangles.N, out n);
            int.TryParse(Triangles.M, out m);
            double width = Board.ActualWidth + Board.Margin.Left + Board.Margin.Right;
            double height = Board.ActualHeight + Board.Margin.Top + Board.Margin.Bottom;
            var widthCount = (int)Math.Round((Board.ActualWidth + Board.Margin.Left * 2) / n);
            var heightCount = (int)Math.Round((Board.ActualHeight + Board.Margin.Top * 2) / m);
            if (clear)
                vertices = new Point[heightCount, widthCount];
            for (int i = 0; i < heightCount; i++)
            {
                for (int j = 0; j < widthCount; j++)
                {
                    if (clear)
                        vertices[i, j] = new Point(j * n, i * m);
                    var point = vertices[i, j];
                    var ellipse = new Ellipse()
                    {
                        Width = 4,
                        Height = 4,
                        Fill = Brushes.Red,
                    };
                    Canvas.SetLeft(ellipse, point.X - 2);
                    Canvas.SetTop(ellipse, point.Y - 2);
                    Board.Children.Add(ellipse);
                    if (j > 0)
                        Board.Children.Add(new Line()
                        {
                            Stroke = Brushes.Black,
                            StrokeThickness = 1,
                            X1 = vertices[i, j - 1].X,
                            Y1 = vertices[i, j - 1].Y,
                            X2 = point.X,
                            Y2 = point.Y
                        });
                    if (i > 0 && j + 1 < widthCount)
                    {
                        Board.Children.Add(new Line()
                        {
                            Stroke = Brushes.Black,
                            StrokeThickness = 1,
                            X1 = point.X,
                            Y1 = point.Y,
                            X2 = vertices[i - 1, j + 1].X,
                            Y2 = vertices[i - 1, j + 1].Y
                        });
                    }
                    if (i > 0)
                    {
                        Board.Children.Add(new Line()
                        {
                            Stroke = Brushes.Black,
                            StrokeThickness = 1,
                            X1 = vertices[i - 1, j].X,
                            Y1 = vertices[i - 1, j].Y,
                            X2 = point.X,
                            Y2 = point.Y
                        });
                    }
                }
            }
            ScanLine();
        }

        private void ScanLine()
        {
            List<(double x, double y)> sortedVertices = new List<(double, double)>();
            foreach (var vertex in vertices)
                sortedVertices.Add((vertex.X, vertex.Y));
            sortedVertices.Sort((el1, el2) => { return (int)(el1.y - el2.y == 0 ? el1.x - el2.x : el1.y - el2.y); });
            double ymin = sortedVertices[0].y;
            double ymax = sortedVertices[sortedVertices.Count - 1].y;
            int i = 0;
            for(int y = (int)ymin; y <= ymax; y++)
            {

            }
        }

        private bool BelongsToCircle(Point vertex, Point circle)
        {
            double d = Math.Sqrt((vertex.X - circle.X) * (vertex.X - circle.X) +
                    (vertex.Y - circle.Y) * (vertex.Y - circle.Y));
            if (d <= 3)
                return true;
            return false;
        }
    }
}
