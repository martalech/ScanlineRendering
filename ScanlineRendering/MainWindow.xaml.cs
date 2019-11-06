using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace ScanlineRendering
{
    public partial class ScanlineRednering : Window, INotifyPropertyChanged
    {
        public FillColorInfo FillColorInfo
        {
            get
            {
                return fillColorInfo;
            }
            set
            {
                if (value != fillColorInfo)
                {
                    fillColorInfo = value;
                    RaisePropertyChanged();
                }
            }
        }

        public FillColorSettings FillColorSettings
        {
            get
            {
                return fillColorSettings;
            }
            set
            {
                if (value != fillColorSettings)
                {
                    fillColorSettings = value;
                    RaisePropertyChanged();
                }
            }
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

        private int mainStride;
        private bool trianglesInfoChanged = false;
        private int direction = -1;

        private TrianglesInfo trianglesInfo;
        private FillColorInfo fillColorInfo;
        private FillColorInfo appliedColorInfo;
        private FillColorSettings fillColorSettings;
        private FillColorSettings appliedColorSettings;

        private Vertex movingPoint = null;
        private List<Triangle> triangles = new List<Triangle>();
        private byte[] NBitmap, ColBitmap;
        private int nBitmapStride, colBitmapStride;
        private Vector3D LVector = new Vector3D(0, 0, 1);

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ScanlineRednering()
        {
            InitializeComponent();
            FillColorInfo = new FillColorInfo(0.5, 0.5, 15, Colors.Red, Colors.White);
            FillColorInfo.PropertyChanged += FillColorInfo_PropertyChanged;
            appliedColorInfo = new FillColorInfo(0.5, 0.5, 1, Colors.Red, Colors.White);
            FillColorSettings = new FillColorSettings(false, true, false, false, true, false);
            appliedColorSettings = new FillColorSettings(false, true, false, false, true, false);
            TrianglesInfo = new TrianglesInfo("80", "60");
            TrianglesInfo.PropertyChanged += TrianglesInfo_PropertyChanged;
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();
        }

        private void FillColorInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName== "Ks")
                FillColorInfo.Kd = 1 - FillColorInfo.Ks;
            else if (e.PropertyName == "Kd")
                FillColorInfo.Ks = 1 - FillColorInfo.Kd;
        }

        private void TrianglesInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            trianglesInfoChanged = true;
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (appliedColorSettings.MovingLight)
            {
                double angle = direction * Math.PI / 180 * 5;
                double x = LVector.X;
                double z = LVector.Z;
                LVector.X = x * Math.Cos(angle) + z * Math.Sin(angle);
                LVector.Z = (-1) * x * Math.Sin(angle) + z * Math.Cos(angle);
                if (direction == -1 && LVector.X < -0.3)
                    direction = 1;
                else if (direction == 1 && LVector.X > 0.3)
                    direction = -1;
                LVector.Normalize();
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

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            var mouse = e.GetPosition(Board);
            bubblePoint = e.GetPosition(Board);
            if (appliedColorSettings.Bubble)
            {
                RepaintTriangles();
            }
            if (e.LeftButton == MouseButtonState.Pressed && movingPoint != null && !appliedColorSettings.MovingLight)
            {
                movingPoint.Coord = new Point(mouse.X, mouse.Y);
                RepaintTriangles(false);
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

        private void OnCanvasLoaded(object sender, RoutedEventArgs e)
        {
            LoadColBitmap(Properties.Resources.bloom_blooming_bright_1131407);
            LoadNBitmap(Properties.Resources.Carpet_01_NRM);
            RepaintTriangles(true);
            trianglesInfoChanged = false;
        }


        private void OnLModeClick(object sender, RoutedEventArgs e)
        {
            LVector = new Vector3D(-0.3, 0, 0.91);
            LVector.Normalize();
        }

        private void OnNoLModeClick(object sender, RoutedEventArgs e)
        {
            LVector = new Vector3D(0, 0, 1);
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
                    double scaleX =  Board.ActualWidth / nBitmap.PixelWidth;
                    double scaleY = Board.ActualHeight / nBitmap.PixelHeight;
                    var nBitmap2 = new TransformedBitmap(nBitmap, new ScaleTransform(scaleX, scaleY));
                    nBitmapStride = ((nBitmap2.PixelWidth * nBitmap2.Format.BitsPerPixel + 7) / 8);
                    int nBitmapSize = nBitmapStride * nBitmap2.PixelHeight;
                    NBitmap = new byte[nBitmapSize];
                    nBitmap2.CopyPixels(NBitmap, nBitmapStride, 0);
                }
                else
                {
                    MessageBox.Show("No file chosen. Applying default normal map");
                    LoadNBitmap(Properties.Resources.Carpet_01_NRM);
                }
            }
            else
            {
                MessageBox.Show("No file chosen. Applying default normal map");
                LoadNBitmap(Properties.Resources.Carpet_01_NRM);
            }
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
                    double scaleY = Board.ActualHeight/ colBitmap.PixelHeight;
                    var colBitmap2 = new TransformedBitmap(colBitmap, new ScaleTransform(scaleX, scaleY));
                    colBitmapStride = ((colBitmap2.PixelWidth * colBitmap2.Format.BitsPerPixel + 7) / 8);
                    int colBitmapSize = colBitmapStride * colBitmap2.PixelHeight;
                    ColBitmap = new byte[colBitmapSize];
                    colBitmap2.CopyPixels(ColBitmap, colBitmapStride, 0);
                }
                else
                {
                    MessageBox.Show("No file chosen. Applying default color texture");
                    LoadColBitmap(Properties.Resources.bloom_blooming_bright_1131407);
                }
            }
            else
            {
                MessageBox.Show("No file chosen. Applying default color texture");
                LoadColBitmap(Properties.Resources.bloom_blooming_bright_1131407);
            }
        }

        private void OnApplyChangesClick(object sender, RoutedEventArgs e)
        {
            if (!IsValid(NSize) || !IsValid(MSize))
            {
                MessageBox.Show("Please enter correct triangles' height and width (larger than 0)");
                return;
            }
            appliedColorInfo = new FillColorInfo(FillColorInfo.Ks, FillColorInfo.Kd, FillColorInfo.M,
                FillColorInfo.ObjectColor, FillColorInfo.LightColor);
            appliedColorSettings = new FillColorSettings(FillColorSettings.KMSliders, FillColorSettings.NormalMap, 
                FillColorSettings.InterpolMode, FillColorSettings.HybridMode, FillColorSettings.ColorFromTexture,
                FillColorSettings.MovingLight, FillColorSettings.NoGrid, FillColorSettings.Bubble);
            if (appliedColorSettings.NormalMap && NBitmap == null)
            {
                MessageBox.Show("No normal map chosen. Setting default normal map.");
                LoadNBitmap(Properties.Resources.Carpet_01_NRM);
            }
            if (appliedColorSettings.ColorFromTexture && ColBitmap == null)
            {
                MessageBox.Show("No color texture chosen. Setting default color texture.");
                LoadColBitmap(Properties.Resources.bloom_blooming_bright_1131407);
            }
            if (trianglesInfoChanged)
                RepaintTriangles(true);
            else
                RepaintTriangles(false);
            trianglesInfoChanged = false;
        }

        private void RepaintTriangles(bool clear = false)
        {
            Board.Children.Clear();
            int n, m;
            int.TryParse(trianglesInfo.N, out n);
            int.TryParse(trianglesInfo.M, out m);
            var widthCount = (int)Math.Round(Board.ActualWidth / n);
            var heightCount = (int)Math.Round(Board.ActualHeight / m);
            if (clear)
            {
                Vertex[,] vertices = new Vertex[heightCount, widthCount];
                triangles = new List<Triangle>();
                for (int i = 0; i < heightCount; i++)
                {
                    for (int j = 0; j < widthCount; j++)
                    {
                        if (i < heightCount && j < widthCount && vertices[i, j] == null)
                            vertices[i, j] = new Vertex(j * n, i * m);
                        if (i > 0 && j > 0)
                        {
                            Vertex leftTop = vertices[i - 1, j - 1];
                            Vertex leftBottom = vertices[i, j - 1];
                            Vertex rightBottom = vertices[i, j];
                            Vertex rightTop = vertices[i - 1, j];
                            Triangle lowerTriangle = new Triangle(new Edge(leftBottom, rightBottom), new Edge(rightBottom, rightTop),
                                new Edge(rightTop, leftBottom));
                            Triangle upperTriangle = new Triangle(new Edge(leftBottom, rightTop), new Edge(rightTop, leftTop),
                                new Edge(leftTop, leftBottom));
                            if (!appliedColorSettings.NoGrid)
                            {
                                upperTriangle.AddTriangleToCanvas(Board);
                                lowerTriangle.AddTriangleToCanvas(Board);
                            }
                            triangles.Add(upperTriangle);
                            triangles.Add(lowerTriangle);
                        }
                    }
                }
            }
            PaintTriangles();
        }

        private void PaintTriangles()
        {
            WriteableBitmap writeableBitmap = new WriteableBitmap((int)Board.ActualWidth, (int)Board.ActualHeight,
                96, 96, PixelFormats.Bgra32, null);
            byte[] pixels = new byte[(int)Board.ActualWidth * (int)Board.ActualHeight * 4];
            mainStride = 4 * (int)Board.ActualWidth;
            Parallel.ForEach(triangles, (triangle) => { ScanLine(triangle, pixels); });
            Int32Rect rect = new Int32Rect(0, 0, (int)Board.ActualWidth, (int)Board.ActualHeight);
            writeableBitmap.WritePixels(rect, pixels, mainStride, 0);
            Image image = new Image();
            image.Source = writeableBitmap;
            Board.Children.Add(image);
            foreach (var triangle in triangles)
            {
                if (!appliedColorSettings.NoGrid)
                    triangle.AddTriangleToCanvas(Board);
            }
        }

        private void ScanLine(Triangle triangle, byte[] pixels)
        {
            double ymin = triangle.Edges.Max((e) => { return e.From.Y; }), miny;
            List<EdgeET>[] ET = new List<EdgeET>[(int)Math.Round(ymin) + 1];
            foreach (var edge in triangle.Edges)
            {
                if (edge.From.Y == edge.To.Y)
                    continue;
                miny = edge.From.Y < edge.To.Y ? edge.To.Y : edge.From.Y;
                if (ET[(int)Math.Round(miny)] == null)
                    ET[(int)Math.Round(miny)] = new List<EdgeET>();
                ET[(int)Math.Round(miny)].Add(new EdgeET(edge));
            }
            int y = -1;
            int i = 0;
            for (i = 0; i < ET.Length; i++)
            {
                var it = ET[i];
                if (it != null)
                {
                    if (y == -1 || i > y)
                        y = i;
                    it.Sort((el1, el2) =>
                    {
                        return (int)(el1.edge.To.X - el2.edge.From.X);
                    });
                }
            }
            List<EdgeET> AET = new List<EdgeET>();
            double kd, ks, m;
            Color c = Colors.Blue;
            Vector3D N = new Vector3D(0, 0, 1);
            if (!appliedColorSettings.ColorFromTexture)
                c = appliedColorInfo.ObjectColor;
            if (appliedColorSettings.KMSliders)
            {
                kd = appliedColorInfo.Kd;
                ks = appliedColorInfo.Ks;
                m = appliedColorInfo.M;
            }
            else
            {
                Random random = new Random();
                kd = random.NextDouble();
                ks = 1 - kd;
                m = random.Next() % 100 + 1;
            }
            while (!(AET.Count == 0 && IsArrayEmpty(ET)))
            {
                if (y > 0 && ET[y] != null)
                {
                    foreach (var e in ET[y])
                        AET.Add(e);
                    ET[y] = null;
                }
                AET.Sort((el1, el2) =>
                {
                    return (int)(el1.xmin - el2.xmin) == 0 ? (int)(el1.ymax - el2.ymax) : (int)(el1.xmin - el2.xmin);
                });
                List<EdgeET> todel = new List<EdgeET>();
                for(int k = 0; k < AET.Count; k++)
                {
                    if (k < AET.Count - 1)
                    {
                        double x1 = AET[k].xmin;
                        double x2 = AET[k + 1].xmin;
                        for (double x = Math.Round(x1); x <= Math.Round(x2); x++)
                        {
                            int index;
                            Vector3D vector = new Vector3D(0, 0, 0);
                            index = (int)Math.Round(x) * 4 + y * colBitmapStride;
                            Vector3D bubbleN = new Vector3D(0, 0, 1);
                            if (appliedColorSettings.Bubble && InsideBubble(new Point(x, y)))
                            {
                                double xx = (x - bubblePoint.X) * (x - bubblePoint.X);
                                double yy = (y - bubblePoint.Y) * (y - bubblePoint.Y);
                                double zz = 50 * 50 - xx - yy;
                                bubbleN = new Vector3D(xx, yy, zz);
                                bubbleN.Normalize();
                            }
                            if (appliedColorSettings.ColorFromTexture)
                                c = Color.FromRgb(ColBitmap[index + 2], ColBitmap[index + 1], ColBitmap[index]);
                            if (appliedColorSettings.InterpolMode)
                            {
                                int wx1 = (int)triangle.Edges[0].From.X;
                                int wx2 = (int)triangle.Edges[1].From.X;
                                int wx3 = (int)triangle.Edges[2].From.X;
                                int wy1 = (int)triangle.Edges[0].From.Y;
                                int wy2 = (int)triangle.Edges[1].From.Y;
                                int wy3 = (int)triangle.Edges[2].From.Y;
                                Vector3D bubbleN1 = new Vector3D(0, 0, 1), bubbleN2 = new Vector3D(0, 0, 1), bubbleN3 = new Vector3D(0, 0, 1);
                                if (appliedColorSettings.Bubble && InsideBubble(new Point(wx1, wy1)))
                                {
                                    double xx1 = (wx1 - bubblePoint.X) * (wx1 - bubblePoint.X);
                                    double yy1 = (wy1 - bubblePoint.Y) * (wy1 - bubblePoint.Y);
                                    double zz1 = 50 * 50 - xx1 - yy1;
                                    bubbleN1 = new Vector3D(xx1, yy1, zz1);
                                    bubbleN1.Normalize();
                                }
                                if (appliedColorSettings.Bubble && InsideBubble(new Point(wx2, wy2)))
                                {
                                    double xx2 = (wx2 - bubblePoint.X) * (wx2 - bubblePoint.X);
                                    double yy2 = (wy2 - bubblePoint.Y) * (wy2 - bubblePoint.Y);
                                    double zz2 = 50 * 50 - xx2 - yy2;
                                    bubbleN2 = new Vector3D(xx2, yy2, zz2);
                                    bubbleN2.Normalize();
                                }
                                if (appliedColorSettings.Bubble && InsideBubble(new Point(wx2, wy2)))
                                {
                                    double xx3 = (wx3 - bubblePoint.X) * (wx3 - bubblePoint.X);
                                    double yy3 = (wy3 - bubblePoint.Y) * (wy3 - bubblePoint.Y);
                                    double zz3 = 50 * 50 - xx3 - yy3;
                                    bubbleN3 = new Vector3D(xx3, yy3, zz3);
                                    bubbleN3.Normalize();
                                }
                                Vector3D v1, v2, v3;
                                if (appliedColorSettings.ColorFromTexture)
                                {
                                    int index1 = wx1 * 4 + wy1 * colBitmapStride;
                                    Color c1 = Color.FromRgb(ColBitmap[index1 + 2], ColBitmap[index1 + 1], ColBitmap[index1]);
                                    int index2 = wx2 * 4 + wy2 * colBitmapStride;
                                    Color c2 = Color.FromRgb(ColBitmap[index2 + 2], ColBitmap[index2 + 1], ColBitmap[index2]);
                                    int index3 = wx3 * 4 + wy3 * colBitmapStride;
                                    Color c3 = Color.FromRgb(ColBitmap[index3 + 2], ColBitmap[index3 + 1], ColBitmap[index3]);
                                    v1 = I(kd, ks, m, c1, bubbleN1, wx1, wy1);
                                    v2 = I(kd, ks, m, c2, bubbleN2, wx2, wy2);
                                    v3 = I(kd, ks, m, c3, bubbleN3, wx3, wy3);
                                }
                                else
                                {
                                    v1 = I(kd, ks, m, c, bubbleN1, wx1, wy1);
                                    v2 = I(kd, ks, m, c, bubbleN2, wx2, wy2);
                                    v3 = I(kd, ks, m, c, bubbleN3, wx3, wy3);
                                }
                                double l1 = CalculateLength((int)x, y, wx1, wy1);
                                double l2 = CalculateLength((int)x, y, wx2, wy2);
                                double l3 = CalculateLength((int)x, y, wx3, wy3);
                                vector = (v1 * l1 + v2 * l2 + v3 * l3) / (l1 + l2 + l3);
                            }
                            else if (appliedColorSettings.HybridMode)
                            {
                                int wx1 = (int)triangle.Edges[0].From.X;
                                int wx2 = (int)triangle.Edges[1].From.X;
                                int wx3 = (int)triangle.Edges[2].From.X;
                                int wy1 = (int)triangle.Edges[0].From.Y;
                                int wy2 = (int)triangle.Edges[1].From.Y;
                                int wy3 = (int)triangle.Edges[2].From.Y;
                                double gamma = (y * wx2 - y * wx1 - wx2 * wy1 + x * wy1 - x * wy2 + wx1 * wy2)
                                    / (wy1 * wx3 - wy1 * wx2 + wx1 * wy2 - wx3 * wy2 + wy3 * wx2 - wy3 * wx1);
                                double beta = (x + gamma * wx1 - gamma * wx3 - wx1) / (wx2 - wx1);
                                double alfa = 1 - beta - gamma;
                                Vector3D v1, v2, v3;
                                                                Vector3D bubbleN1 = new Vector3D(0, 0, 1), bubbleN2 = new Vector3D(0, 0, 1), bubbleN3 = new Vector3D(0, 0, 1);
                                if (appliedColorSettings.Bubble && InsideBubble(new Point(wx1, wy1)))
                                {
                                    double xx1 = (wx1 - bubblePoint.X) * (wx1 - bubblePoint.X);
                                    double yy1 = (wy1 - bubblePoint.Y) * (wy1 - bubblePoint.Y);
                                    double zz1 = 50 * 50 - xx1 - yy1;
                                    bubbleN1 = new Vector3D(xx1, yy1, zz1);
                                    bubbleN1.Normalize();
                                }
                                if (appliedColorSettings.Bubble && InsideBubble(new Point(wx2, wy2)))
                                {
                                    double xx2 = (wx2 - bubblePoint.X) * (wx2 - bubblePoint.X);
                                    double yy2 = (wy2 - bubblePoint.Y) * (wy2 - bubblePoint.Y);
                                    double zz2 = 50 * 50 - xx2 - yy2;
                                    bubbleN2 = new Vector3D(xx2, yy2, zz2);
                                    bubbleN2.Normalize();
                                }
                                if (appliedColorSettings.Bubble && InsideBubble(new Point(wx2, wy2)))
                                {
                                    double xx3 = (wx3 - bubblePoint.X) * (wx3 - bubblePoint.X);
                                    double yy3 = (wy3 - bubblePoint.Y) * (wy3 - bubblePoint.Y);
                                    double zz3 = 50 * 50 - xx3 - yy3;
                                    bubbleN3 = new Vector3D(xx3, yy3, zz3);
                                    bubbleN3.Normalize();
                                }
                                bubbleN3.Normalize();
                                if (appliedColorSettings.ColorFromTexture)
                                {
                                    int index1 = wx1 * 4 + wy1 * colBitmapStride;
                                    Color c1 = Color.FromRgb(ColBitmap[index1 + 2], ColBitmap[index1 + 1], ColBitmap[index1]);
                                    int index2 = wx2 * 4 + wy2 * colBitmapStride;
                                    Color c2 = Color.FromRgb(ColBitmap[index2 + 2], ColBitmap[index2 + 1], ColBitmap[index2]);
                                    int index3 = wx3 * 4 + wy3 * colBitmapStride;
                                    Color c3 = Color.FromRgb(ColBitmap[index3 + 2], ColBitmap[index3 + 1], ColBitmap[index3]);
                                    v1 = I(kd, ks, m, c1, bubbleN1, wx1, wy1);
                                    v2 = I(kd, ks, m, c2, bubbleN2, wx2, wy2);
                                    v3 = I(kd, ks, m, c3, bubbleN3, wx3, wy3);
                                }
                                else
                                {
                                    v1 = I(kd, ks, m, c, bubbleN1, wx1, wy1);
                                    v2 = I(kd, ks, m, c, bubbleN2, wx2, wy2);
                                    v3 = I(kd, ks, m, c, bubbleN3, wx3, wy3);
                                }
                                vector = alfa * v1 + beta * v2 + gamma * v3;
                            }
                            else
                            {
                                vector = I(kd, ks, m, c, bubbleN, (int)Math.Round(x), y);
                            }
                            Color col = Color.FromRgb((byte)vector.X, (byte)vector.Y, (byte)vector.Z);
                            index = (int)Math.Round(x) * 4 + y * mainStride;
                            pixels[index] = col.B;
                            pixels[index + 1] = col.G;
                            pixels[index + 2] = col.R;
                            pixels[index + 3] = col.A;
                        }
                    }
                }
                for (int k = 0; k < AET.Count; k++)
                {
                    if (Math.Round(AET[k].ymax) == y)
                        todel.Add(AET[k]);
                }
                y--;
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

        private void LoadColBitmap(System.Drawing.Bitmap bitmap)
        {
            BitmapImage colBitmap = ToBitmapImage(bitmap);
            double scaleX = Board.ActualWidth / colBitmap.PixelWidth;
            double scaleY = Board.ActualHeight / colBitmap.PixelHeight;
            var transformedColBitmap = new TransformedBitmap(colBitmap, new ScaleTransform(scaleX, scaleY));
            colBitmapStride = ((transformedColBitmap.PixelWidth * transformedColBitmap.Format.BitsPerPixel + 7) / 8);
            int colBitmapSize = colBitmapStride * transformedColBitmap.PixelHeight;
            ColBitmap = new byte[colBitmapSize];
            transformedColBitmap.CopyPixels(ColBitmap, colBitmapStride, 0);
        }

        private void LoadNBitmap(System.Drawing.Bitmap bitmap)
        {
            BitmapImage nBitmap = ToBitmapImage(bitmap);
            double scaleX = Board.ActualWidth / nBitmap.PixelWidth;
            double scaleY = Board.ActualHeight / nBitmap.PixelHeight;
            var transformedColBitmap = new TransformedBitmap(nBitmap, new ScaleTransform(scaleX, scaleY));
            nBitmapStride = ((transformedColBitmap.PixelWidth * transformedColBitmap.Format.BitsPerPixel + 7) / 8);
            int nBitmapSize = nBitmapStride * transformedColBitmap.PixelHeight;
            NBitmap = new byte[nBitmapSize];
            transformedColBitmap.CopyPixels(NBitmap, nBitmapStride, 0);
        }

        private Vector3D I(double kd, double ks, double m, Color c, Vector3D N, int x, int y)
        {
            Vector3D V, IO, IL, R;
            if (appliedColorSettings.NormalMap)
            {
                int index = x * 4 + y * nBitmapStride;
                N = new Vector3D(NBitmap[index + 2] == 0 ? -1 : ((double)NBitmap[index + 2] - 127) / 128,
                    NBitmap[index + 1] == 0 ? -1 : ((double)NBitmap[index + 1] - 127) / 128,
                    (double)NBitmap[index] / 255);
                N.Normalize();
            }
            V = new Vector3D(0, 0, 1);
            Color light = appliedColorInfo.LightColor;
            IL = new Vector3D(light.R / 255, light.G / 255, light.B / 255);
            IO = new Vector3D(c.R, c.G, c.B);
            Vector3D L = LVector;
            R = 2 * N * Angle(N, L) - L;
            R.Normalize();
            double angle1 = Angle(N, L);
            double angle2 = Math.Pow(Angle(V, R), m);
            return new Vector3D((kd * IL.X * IO.X * angle1) + (ks * IL.X * IO.X * angle2),
                (kd * IL.Y * IO.Y * angle1) + (ks * IL.Y * IO.Y * angle2),
                (kd * IL.Z * IO.Z * angle1) + (ks * IL.Z * IO.Z * angle2));
        }

        private void OnConstNRadioButonClick(object sender, RoutedEventArgs e)
        {
            if (!appliedColorSettings.MovingLight)
                NBitmap = null;
        }

        private void OnConstColRadioButtonClick(object sender, RoutedEventArgs e)
        {
            if (!appliedColorSettings.MovingLight)
                ColBitmap = null;
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

        private bool IsValid(DependencyObject obj)
        {
            return !Validation.GetHasError(obj) && LogicalTreeHelper.GetChildren(obj).OfType<DependencyObject>()
                .All(IsValid);
        }

        private int CalculateLength(int x1, int y1, int x2, int y2)
        {
            return (int)Math.Sqrt((Math.Abs(y2 - y1) * Math.Abs(y2 - y1))
                + (Math.Abs(x2 - x1) * Math.Abs(x2 - x1)));
        }

        private bool IsArrayEmpty(List<EdgeET>[] array)
        {
            foreach (var a in array)
                if (a != null)
                    return false;
            return true;
        }

        Point bubblePoint = new Point();

        private bool InsideBubble(Point point)
        {
            double d = Math.Sqrt((point.X - bubblePoint.X) * (point.X - bubblePoint.X) +
                (point.Y - bubblePoint.Y) * (point.Y - bubblePoint.Y));
            if (d <= 50)
                return true;
            return false;
        }

        private bool BelongsToCircle(Point vertex, Point circle)
        {
            double d = Math.Sqrt((vertex.X - circle.X) * (vertex.X - circle.X) +
                    (vertex.Y - circle.Y) * (vertex.Y - circle.Y));
            if (d <= 4)
                return true;
            return false;
        }

        private double Angle(Vector3D v1, Vector3D v2)
        {
            return Math.Max(0, Vector3D.DotProduct(v1, v2));
        }
    }
}
