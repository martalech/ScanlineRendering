using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScanlineRendering
{
    public class Triangle
    {
        public List<Edge> Edges { get; set; }
        
        public Triangle()
        {
            Edges = new List<Edge>();
        }

        public Triangle(params Edge[] edges)
        {
            Edges = new List<Edge>();
            foreach (var edge in edges)
            {
                edge.From.Triangles.Add(this);
                edge.To.Triangles.Add(this);
                Edges.Add(edge);
            }
        }

        public void AddTriangleToCanvas(Canvas canvas)
        {
            foreach (var edge in Edges)
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
                canvas.Children.Add(line);
            }
        }
    }
}
