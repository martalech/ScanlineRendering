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
        //private List<Vertex> vertices;
        private TrianglesInfo trianglesInfo;
        private Vertex movingPoint = null;
        //private (int i, int j)? movingPoint = null;
        //private Edge movingEdge = null;

        private List<EdgeET>[] ET = null;
        private List<EdgeET> AET = null;
        private List<Triangle> triangles = new List<Triangle>();
        private Vertex[,] vertices;
        //private SortedList<int, (int ymax, double m)> AET = new SortedList<int, (int, double)>();

        public event PropertyChangedEventHandler PropertyChanged;
        public TrianglesInfo Triangles
        {
            get
            {
                return trianglesInfo;
            }
            set
            {
                if (value != trianglesInfo)
                {
                    trianglesInfo = value;
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
            foreach (var triangle in triangles)
            {
                foreach (var edge in triangle.Edges)
                {
                    if (BelongsToCircle(mouse, edge.From.Coord))
                    {
                        movingPoint = edge.From;
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
                movingPoint.Coord = new Point(mouse.X, mouse.Y);
                RepaintTriangles(false);
                //foreach (var t in movingPoint.Triangles)
                //    ScanLine(t);
                //foreach (var t in movingPoint.Triangles)
                //    t.AddTriangleToCanvas(Board);
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
            //PaintTriangles();
            if (clear)
            {
                vertices = new Vertex[heightCount, widthCount];
                triangles = new List<Triangle>();
                for (int i = 0; i < heightCount; i++)
                {
                    for (int j = 0; j < widthCount - 1; j++)
                    {
                        if (vertices[i, j] == null)
                            vertices[i, j] = new Vertex(j * n, i * m);
                        if (vertices[i, j + 1] == null)
                            vertices[i, j + 1] = new Vertex((j + 1) * n, i * m);
                        //var point = new Point(j * n, i * m);
                        //var ellipse = new Ellipse()
                        //{
                        //    Width = 4,
                        //    Height = 4,
                        //    Fill = Brushes.Red,
                        //};
                        //Canvas.SetLeft(ellipse, point.X - 2);
                        //Canvas.SetTop(ellipse, point.Y - 2);
                        //Board.Children.Add(ellipse);
                        //if (j > 0)
                        //{

                        //    Board.Children.Add(new Line()
                        //    {
                        //        Stroke = Brushes.Black,
                        //        StrokeThickness = 1,
                        //        X1 = (j - 1) * n,
                        //        Y1 = i * m,
                        //        X2 = point.X,
                        //        Y2 = point.Y
                        //    });
                        //}
                        if (i > 0 && j > 0 && j + 1 < widthCount)
                        {
                            //Board.Children.Add(new Line()
                            //{
                            //    Stroke = Brushes.Black,
                            //    StrokeThickness = 1,
                            //    X1 = point.X,
                            //    Y1 = point.Y,
                            //    X2 = (j + 1) * n,
                            //    Y2 = (i - 1) * m
                            //});
                            //Vertex leftTop = new Vertex(j * n, (i - 1) * m);
                            //Vertex leftBottom = new Vertex(j * n, i * m);
                            //Vertex rightBottom = new Vertex((j + 1) * n, i * m);
                            //Vertex rightTop = new Vertex((j + 1) * n, (i - 1) * m);
                            Vertex leftTop = vertices[i - 1, j];
                            Vertex leftBottom = vertices[i, j];
                            Vertex rightBottom = vertices[i, j + 1];
                            Vertex rightTop = vertices[i - 1, j + 1];
                            Triangle lowerTriangle = new Triangle(new Edge(leftBottom, rightBottom), new Edge(rightBottom, rightTop), new Edge(rightTop, leftBottom));
                            Triangle upperTriangle = new Triangle(new Edge(leftBottom, rightTop), new Edge(rightTop, leftTop), new Edge(leftTop, leftBottom));
                            upperTriangle.AddTriangleToCanvas(Board);
                            lowerTriangle.AddTriangleToCanvas(Board);
                            triangles.Add(upperTriangle);
                            triangles.Add(lowerTriangle);
                        }
                        //if (i > 0)
                        //{
                        //    Board.Children.Add(new Line()
                        //    {
                        //        Stroke = Brushes.Black,
                        //        StrokeThickness = 1,
                        //        X1 = j * n,
                        //        Y1 = (i - 1) * m,
                        //        X2 = point.X,
                        //        Y2 = point.Y
                        //    });

                        //}
                    }
                }
            }
            else
            {
                //foreach (var triangle in triangles)
                //    triangle.AddTriangleToCanvas(Board);
            }
            PaintTriangles();
        }

        private void PaintTriangles()
        {
            foreach (var triangle in triangles)
                ScanLine(triangle);
            foreach (var triangle in triangles)
                triangle.AddTriangleToCanvas(Board);
        }

        private void ScanLine(Triangle triangle)
        {
            double ymin = triangle.Edges.Max((e) => { return e.From.Y; });
            double ymax = triangle.Edges.Min((e) => { return e.From.Y; });
            ET = new List<EdgeET>[(int)(ymin + 1)];
            double miny, maxy;
            double minx, maxx;
            foreach (var edge in triangle.Edges)
            {
                if (edge.From.Y == edge.To.Y)
                    continue;
                miny = edge.From.Y < edge.To.Y ? edge.To.Y : edge.From.Y;
                //maxy = miny == edge.From.Y ? edge.To.Y : edge.From.Y;
                minx = miny == edge.From.Y ? edge.From.X : edge.To.X;
                if (ET[(int)(miny)] == null)
                    ET[(int)(miny)] = new List<EdgeET>();
                ET[(int)(miny)].Add(new EdgeET(edge));
            }
            int i = 0;
            int indmin = -1;
            foreach(var it in ET)
            {
                if (it != null)
                {
                    if (indmin == -1 || i > indmin)
                        indmin = i;
                    it.Sort((el1, el2) =>
                    {
                        return (int)(el1.edge.To.X - el2.edge.From.X);
                    });
                }
                i++;
            }
            AET = new List<EdgeET>();
            int elo = indmin;
            while (!(AET.Count == 0 && IsArrayEmpty(ET)))
            {
                if (ET[indmin] != null)
                {
                    foreach (var e in ET[indmin])
                        AET.Add(e);
                    ET[indmin] = null;
                }
                AET.Sort((el1, el2) =>
                {
                    return (int)(el1.xmin - el2.xmin);
                });
                List<EdgeET> todel = new List<EdgeET>();
                for(int k = 0; k < AET.Count; k += 2)
                {
                    if (k < AET.Count - 1)
                    {
                        double x1 = AET[k].xmin;
                        double x2 = AET[k + 1].xmin;
                        //for (double x = x1; x < x2; x++)
                        //{
                        //    var rec = new Rectangle();
                        //    Canvas.SetTop(rec, indmin);
                        //    Canvas.SetLeft(rec, x);
                        //    rec.Width = 1;
                        //    rec.Height = 1;
                        //    rec.Fill = new SolidColorBrush(Colors.Red);
                        //    Board.Children.Add(rec);
                        //}
                        Board.Children.Add(new Line()
                        {
                            Stroke = Brushes.Red,
                            StrokeThickness = 1.5,
                            X1 = x1,
                            Y1 = indmin,
                            X2 = x2,
                            Y2 = indmin
                        });
                    }
                    if (AET[k].ymax == indmin)
                        todel.Add(AET[k]);
                    if (k < AET.Count - 1 && AET[k + 1].ymax == indmin)
                        todel.Add(AET[k + 1]);

                }
                foreach(var t in todel)
                {
                    AET.Remove(t);
                }
                indmin--;
                foreach(var e in AET)
                {
                    e.xmin -= e.m;
                }
            }
        }

        private bool IsArrayEmpty(List<EdgeET>[] array)
        {
            foreach (var a in array)
                if (a != null)
                    return false;
            return true;
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
