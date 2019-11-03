using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Resources;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

public class VisualHost: UIElement
{
    public Visual Visual { get; set; }

    protected override int VisualChildrenCount
    {
        get { return Visual != null ? 1 : 0; }
    }

    protected override Visual GetVisualChild(int index)
    {
        return Visual;
    }
}

namespace ScanlineRendering
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private TrianglesInfo trianglesInfo;
        private Vertex movingPoint = null;

        private List<EdgeET>[] ET = null;
        private List<EdgeET> AET = null;
        private List<Triangle> triangles = new List<Triangle>();
        private int n1, m1, ncount, mcount;
        private Vertex[,] vertices;

        public bool KMMode { get; set; } = false;
        public bool NMode { get; set; } = true;
        public bool InterpolMode { get; set; } = false;
        public bool HybridMode { get; set; } = false;
        public bool IoMode { get; set; } = true;
        public bool LMode { get; set; } = false;

        public int Stride { get; set; }
        bool trianglesinfochanged = false;

        Vector3D LVector = new Vector3D(0, 0, 1);
        Vector3D ILVector = new Vector3D(1, 1, 1);

        private byte[] NBitmap, ColBitmap;
        private int nBitmapStride, colBitmapStride;

        public event PropertyChangedEventHandler PropertyChanged;

        private double Angle(Vector3D v1, Vector3D v2)
        {
            return Vector3D.DotProduct(v1, v2);
        }

        public TrianglesInfo TrianglesInfo
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

        public void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private Vector3D I(double kd, double ks, double m, Color c, Vector3D N, int x, int y)
        {
            Vector3D V, IO, IL, R;
            V = new Vector3D(0, 0, 1);
            IL = ILVector;
            IO = new Vector3D(c.R, c.G, c.B);
            Vector3D L = LVector;
            R = 2 * N - L;
            var lol = Vector3D.AngleBetween(N, L);
            double angle1 = Math.Cos(Angle(N, L));
            double angle2 = Math.Pow(Math.Cos(Angle(V, R)), m);
            return new Vector3D((kd * IL.X * IO.X * angle1) + (ks * IL.X * IO.X * angle2),
                (kd * IL.Y * IO.Y * angle1) + (ks * IL.Y * IO.Y * angle2),
                (kd * IL.Z * IO.Z * angle1) + (ks * IL.Z * IO.Z * angle2));
        }

        public MainWindow()
        {
            InitializeComponent();
            TrianglesInfo = new TrianglesInfo("70", "40");
            TrianglesInfo.PropertyChanged += TrianglesInfo_PropertyChanged;
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
        }

        private void TrianglesInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            trianglesinfochanged = true;
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (LMode)
            {
                double angle = Math.PI / 180 * 20;
                LVector.X = LVector.X * Math.Cos(angle) - LVector.Y * Math.Sin(angle);
                LVector.Y = LVector.X * Math.Sin(angle) + LVector.Y * Math.Cos(angle);
                RepaintTriangles();
            }
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
            LoadColBitmap(Properties.Resources.bloom_blooming_bright_1131407);
            LoadNBitmap(Properties.Resources.Carpet_01_NRM);
            RepaintTriangles(true);
            trianglesinfochanged = false;
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            //e.Handled = true;
            var mouse = e.GetPosition(Board);
            if (e.LeftButton == MouseButtonState.Pressed && movingPoint != null)
            {
                movingPoint.Coord = new Point(mouse.X, mouse.Y);
                RepaintTriangles(false);
                //List<Line> todel = new List<Line>();
                //foreach (var line in Board.Children)
                //{
                //    if (line is Line)
                //    {
                //        todel.Add(line as Line);
                //    }
                //}
                //foreach (var el in todel)
                //{
                //    Board.Children.Remove(el);
                //}
                //foreach (var t in trriangles)
                //    t.AddTriangleToCanvas(Board);
            }
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            var mouse = e.GetPosition(Board);
            //if (e.LeftButton == MouseButtonState.Pressed && movingPoint != null)
            //{
            //    movingPoint.Coord = new Point(mouse.X, mouse.Y);
            //    RepaintTriangles(false);
            //    //foreach (var t in movingPoint.Triangles)
            //    //    ScanLine(t);
            //    //foreach (var t in movingPoint.Triangles)
            //    //    t.AddTriangleToCanvas(Board);
            //}
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
            int.TryParse(trianglesInfo.N, out n);
            int.TryParse(trianglesInfo.M, out m);
            double width = Board.ActualWidth + Board.Margin.Left + Board.Margin.Right;
            double height = Board.ActualHeight + Board.Margin.Top + Board.Margin.Bottom;
            var widthCount = (int)Math.Round(Board.ActualWidth / n);
            var heightCount = (int)Math.Round(Board.ActualHeight / m);
            n1 = n;
            m1 = m;
            ncount = widthCount;
            mcount = heightCount;
            //PaintTriangles();
            if (clear)
            {
                vertices = new Vertex[heightCount, widthCount];
                triangles = new List<Triangle>();
                for (int i = 0; i < heightCount; i++)
                {
                    for (int j = 0; j < widthCount; j++)
                    {
                        if (i < heightCount && j < widthCount && vertices[i, j] == null)
                            vertices[i, j] = new Vertex(j * n, i * m);
                        //if (vertices[i, j + 1] == null)
                        //    vertices[i, j + 1] = new Vertex((j + 1) * n, i * m);
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
                        if (i > 0 && j > 0)
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
                            Vertex leftTop = vertices[i - 1, j - 1];
                            Vertex leftBottom = vertices[i, j - 1];
                            Vertex rightBottom = vertices[i, j];
                            Vertex rightTop = vertices[i - 1, j];
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
            WriteableBitmap writeableBitmap = new WriteableBitmap((int)Board.ActualWidth, (int)Board.ActualHeight,
                96, 96, PixelFormats.Bgra32, null);
            byte[] pixels = new byte[(int)Board.ActualWidth * (int)Board.ActualHeight * 4];
            Stride = 4 * (int)Board.ActualWidth;
            foreach (var triangle in triangles)
                ScanLine(triangle, pixels);
            Int32Rect rect = new Int32Rect(0, 0, (int)Board.ActualWidth, (int)Board.ActualHeight);
            writeableBitmap.WritePixels(rect, pixels, Stride, 0);
            Image image = new Image();
            image.Source = writeableBitmap;
            Board.Children.Add(image);
            foreach (var triangle in triangles)
            {
                foreach (var edge in triangle.Edges)
                {
                    var line = new Line()
                    {
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        X1 = edge.From.X,
                        Y1 = edge.From.Y,
                        X2 = edge.To.X,
                        Y2 = edge.To.Y
                    };
                    Board.Children.Add(line);
                }
            }
        }

        private void ScanLine(Triangle triangle, byte[] pixels)
        {
            double ymin = triangle.Edges.Max((e) => { return e.From.Y; });
            double ymax = triangle.Edges.Min((e) => { return e.From.Y; });
            ET = new List<EdgeET>[(int)Math.Round(ymin) + 1];
            double miny, minx;
            foreach (var edge in triangle.Edges)
            {
                if (edge.From.Y == edge.To.Y)
                    continue;
                miny = edge.From.Y < edge.To.Y ? edge.To.Y : edge.From.Y;
                minx = miny == edge.From.Y ? edge.From.X : edge.To.X;
                if (ET[(int)Math.Round(miny)] == null)
                    ET[(int)Math.Round(miny)] = new List<EdgeET>();
                ET[(int)Math.Round(miny)].Add(new EdgeET(edge));
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
            Image Image = new Image();
            double kd, ks, m;
            Color c = Colors.Blue;
            Vector3D N = new Vector3D(0, 0, 1);
            if (!IoMode)
            {
                if (ColPicker.SelectedColor.HasValue)
                    c = ColPicker.SelectedColor.Value;
                else
                    c = Colors.Blue;
            }
            if (KMMode)
            {
                kd = SliderKd.Value;
                ks = SliderKs.Value;
                m = SliderM.Value;
            }
            else
            {
                Random random = new Random();
                kd = random.NextDouble();
                ks = random.NextDouble();
                m = random.Next() % 100 + 1;
            }
            while (!(AET.Count == 0 && IsArrayEmpty(ET)))
            {
                if (indmin > 0 && ET[indmin] != null)
                {
                    foreach (var e in ET[indmin])
                        AET.Add(e);
                    ET[indmin] = null;
                }
                AET.Sort((el1, el2) =>
                {
                    return (int)(el1.xmin - el2.xmin) == 0 ? (int)(el1.ymax - el2.ymax) : (int)(el1.xmin - el2.xmin);
                });
                List<EdgeET> todel = new List<EdgeET>();
                for(int k = 0; k < AET.Count; k++)
                {
                    double xx1, xx2;
                    if (k < AET.Count - 1)
                    {
                        double x1 = AET[k].xmin;
                        double x2 = AET[k + 1].xmin;
                        xx1 = x1;
                        xx2 = x2;
                        for (double x = Math.Round(x1); x <= Math.Round(x2); x++)
                        {
                            Color col;
                            if (IoMode)
                            {
                                int index = (int)Math.Round(x) * 4 + indmin * colBitmapStride;
                                c = Color.FromRgb(ColBitmap[index + 2], ColBitmap[index + 1], ColBitmap[index]);
                            }
                            if (NMode)
                            {
                                int index = (int)Math.Round(x) * 4 + indmin * nBitmapStride;
                                Vector3D v3d = new Vector3D(NBitmap[index + 2], NBitmap[index + 1], NBitmap[index]);
                                N = new Vector3D((NBitmap[index + 2] == 0 ? -1 : (double)NBitmap[index + 2] - 127) / 128, (NBitmap[index + 1] == 0 ? -1 : 
                                    (double)NBitmap[index + 1] - 127) / 128, (double)NBitmap[index] / 255);
                            }
                            Vector3D vector = new Vector3D(0, 0, 0);
                            if (!HybridMode && !InterpolMode)
                                vector = I(kd, ks, m, c, N, (int)Math.Round(x), indmin);
                            else if (InterpolMode && IoMode)
                            {
                                int wx1 = (int)triangle.Edges[0].From.X;
                                int wx2 = (int)triangle.Edges[1].From.X;
                                int wx3 = (int)triangle.Edges[2].From.X;
                                int wy1 = (int)triangle.Edges[0].From.Y;
                                int wy2 = (int)triangle.Edges[1].From.Y;
                                int wy3 = (int)triangle.Edges[2].From.Y;
                                int index1 = wx1 * 4 + wy1 * colBitmapStride;
                                Color c1 = Color.FromRgb(ColBitmap[index1 + 2], ColBitmap[index1 + 1], ColBitmap[index1]);
                                int index2 = wx2 * 4 + wy2 * colBitmapStride;
                                Color c2 = Color.FromRgb(ColBitmap[index2 + 2], ColBitmap[index2 + 1], ColBitmap[index2]);
                                int index3 = wx3 * 4 + wy3 * colBitmapStride;
                                Color c3 = Color.FromRgb(ColBitmap[index3 + 2], ColBitmap[index3 + 1], ColBitmap[index3]);
                                Vector3D v1 = I(kd, ks, m, c1, N, wx1, wy1);
                                Vector3D v2 = I(kd, ks, m, c2, N, wx2, wy2);
                                Vector3D v3 = I(kd, ks, m, c3, N, wx3, wy3);
                                double l1 = CalculateLength((int)x, indmin, wx1, wy1);
                                double l2 = CalculateLength((int)x, indmin, wx2, wy2);
                                double l3 = CalculateLength((int)x, indmin, wx3, wy3);
                                vector = (v1 *l1 + v2 * l2 + v3 * l3) / (l1 + l2 + l3);
                            }
                            else if (HybridMode && IoMode)
                            {
                                int wx1 = (int)triangle.Edges[0].From.X;
                                int wx2 = (int)triangle.Edges[1].From.X;
                                int wx3 = (int)triangle.Edges[2].From.X;
                                int wy1 = (int)triangle.Edges[0].From.Y;
                                int wy2 = (int)triangle.Edges[1].From.Y;
                                int wy3 = (int)triangle.Edges[2].From.Y;
                                double gamma = (indmin * wx2 - indmin * wx1 - wx2 * wy1 + x * wy1 - x * wy2 + wx1 * wy2)
                                    / (wy1 * wx3 - wy1 * wx2 + wx1 * wy2 - wx3 * wy2 + wy3 * wx2 - wy3 * wx1);
                                double beta = (x + gamma * wx1 - gamma * wx3 - wx1) / (wx2 - wx1);
                                double alfa = 1 - beta - gamma;

                                int index1 = wx1 * 4 + wy1 * colBitmapStride;
                                Color c1 = Color.FromRgb(ColBitmap[index1 + 2], ColBitmap[index1 + 1], ColBitmap[index1]);
                                int index2 = wx2 * 4 + wy2 * colBitmapStride;
                                Color c2 = Color.FromRgb(ColBitmap[index2 + 2], ColBitmap[index2 + 1], ColBitmap[index2]);
                                int index3 = wx3 * 4 + wy3 * colBitmapStride;
                                Color c3 = Color.FromRgb(ColBitmap[index3 + 2], ColBitmap[index3 + 1], ColBitmap[index3]);
                                Vector3D v1 = I(kd, ks, m, c1, N, wx1, wy1);
                                Vector3D v2 = I(kd, ks, m, c2, N, wx2, wy2);
                                Vector3D v3 = I(kd, ks, m, c3, N, wx3, wy3);
                                vector = alfa * v1 + beta * v2 + gamma * v3;
                            }
                            col = Color.FromRgb((byte)vector.X, (byte)vector.Y, (byte)vector.Z);
                            int indexx = (int)Math.Round(x) * 4 + indmin * Stride;
                            pixels[indexx] = col.B;
                            pixels[indexx + 1] = col.G;
                            pixels[indexx + 2] = col.R;
                            pixels[indexx + 3] = col.A;
                        }
                    }
                }
                for (int k = 0; k < AET.Count; k++)
                {
                    if (Math.Round(AET[k].ymax) == indmin)
                        todel.Add(AET[k]);
                }
                indmin--;
                foreach (var e in AET)
                {
                    e.xmin -= e.m;
                }
                foreach (var t in todel)
                {
                    AET.Remove(t);
                }
            }
        }

        private void OnLModeClick(object sender, RoutedEventArgs e)
        {
            LVector = new Vector3D(0.3, 0.2, 1);
        }

        private void OnNoLModeClick(object sender, RoutedEventArgs e)
        {
            LVector = new Vector3D(0, 0, 1);
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
            if (d <= 4)
                return true;
            return false;
        }

        private void OnLoadNButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Images (*.png;*.jpeg;*.bmp;*.jpg)|*.png;*.jpeg;*.bmp;*.jpg";
            if (openFileDialog.ShowDialog() == true)
            {
                string filepath = openFileDialog.FileName;
                if (filepath != null && filepath.Length > 0)
                {
                    BitmapImage nBitmap = new BitmapImage(new Uri(filepath));
                    double scaleX = Board.ActualWidth / nBitmap.PixelWidth;
                    double scaleY = Board.ActualHeight / nBitmap.PixelHeight;
                    var nBitmap2 = new TransformedBitmap(nBitmap, new ScaleTransform(scaleX, scaleY));
                    nBitmapStride = ((nBitmap2.PixelWidth * nBitmap2.Format.BitsPerPixel + 7) / 8);
                    int nBitmapSize = nBitmapStride * nBitmap2.PixelHeight;
                    NBitmap = new byte[nBitmapSize];
                    nBitmap2.CopyPixels(NBitmap, nBitmapStride, 0);
                }
            }
        }

        private void OnApplyChangesClick(object sender, RoutedEventArgs e)
        {
            var col = ILColPicker.SelectedColor;
            if (col.HasValue)
            {
                ILVector = new Vector3D((double)col.Value.R/255, (double)col.Value.G/255, (double)col.Value.B/255);
            }
            if (trianglesinfochanged)
                RepaintTriangles(true);
            else
                RepaintTriangles(false);
            trianglesinfochanged = false;
        }

        private void OnLoadColorButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Images (*.png;*.jpeg;*.bmp;*.jpg)|*.png;*.jpeg;*.bmp;*.jpg";
            if (openFileDialog.ShowDialog() == true)
            {
                string filepath = openFileDialog.FileName;
                if (filepath != null && filepath.Length > 0)
                {
                    BitmapImage colBitmap = new BitmapImage(new Uri(filepath));
                    double scaleX = Board.ActualWidth / colBitmap.PixelWidth;
                    double scaleY = Board.ActualHeight / colBitmap.PixelHeight;
                    var colBitmap2 = new TransformedBitmap(colBitmap, new ScaleTransform(scaleX, scaleY));
                    colBitmapStride = ((colBitmap2.PixelWidth * colBitmap2.Format.BitsPerPixel + 7) / 8);
                    int colBitmapSize = colBitmapStride * colBitmap2.PixelHeight;
                    ColBitmap = new byte[colBitmapSize];
                    colBitmap2.CopyPixels(ColBitmap, colBitmapStride, 0);
                }
            }
        }

        private void LoadColBitmap(System.Drawing.Bitmap bitmap)
        {
            BitmapImage colBitmap = ToBitmapImage(bitmap);
            double scaleX = Board.ActualWidth / colBitmap.PixelWidth;
            double scaleY = Board.ActualHeight / colBitmap.PixelHeight;
            var colBitmap2 = new TransformedBitmap(colBitmap, new ScaleTransform(scaleX, scaleY));
            colBitmapStride = ((colBitmap2.PixelWidth * colBitmap2.Format.BitsPerPixel + 7) / 8);
            int colBitmapSize = colBitmapStride * colBitmap2.PixelHeight;
            ColBitmap = new byte[colBitmapSize];
            colBitmap2.CopyPixels(ColBitmap, colBitmapStride, 0);
        }

        private void LoadNBitmap(System.Drawing.Bitmap bitmap)
        {
            BitmapImage nBitmap = ToBitmapImage(bitmap);
            double scaleX = Board.ActualWidth / nBitmap.PixelWidth;
            double scaleY = Board.ActualHeight / nBitmap.PixelHeight;
            var nBitmap2 = new TransformedBitmap(nBitmap, new ScaleTransform(scaleX, scaleY));
            nBitmapStride = ((nBitmap2.PixelWidth * nBitmap2.Format.BitsPerPixel + 7) / 8);
            int nBitmapSize = nBitmapStride * nBitmap2.PixelHeight;
            NBitmap = new byte[nBitmapSize];
            nBitmap2.CopyPixels(NBitmap, nBitmapStride, 0);
        }

        private int CalculateLength(int x1, int y1, int x2, int y2)
        {
            return (int)Math.Sqrt((Math.Abs(y2 - y1) * Math.Abs(y2 - y1))
                + (Math.Abs(x2 - x1) * Math.Abs(x2 - x1)));
        }

        public static BitmapImage ToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Jpeg);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }
    }
}
