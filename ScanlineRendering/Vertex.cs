using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScanlineRendering
{
    public class Vertex
    {
        private Point coord;
        public HashSet<Triangle> Triangles { get; set; }

        public Point Coord
        {
            get
            {
                return coord;
            }
            set
            {
                if (value != coord)
                    coord = value;
            }
        }

        public double X
        {
            get
            {
                return coord.X;
            }
            set
            {
                if (value != coord.X)
                    coord.X = value;
            }
        }

        public double Y
        {
            get
            {
                return coord.Y;
            }
            set
            {
                if (value != coord.Y)
                    coord.Y = value;
            }
        }

        public Vertex(double x, double y)
        {
            Triangles = new HashSet<Triangle>();
            coord = new Point(x, y);
        }
    }
}
