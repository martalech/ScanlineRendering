using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanlineRendering
{
    public class EdgeET
    {
        public double ymax;
        public double xmin;
        public double m;
        public Edge edge;

        public EdgeET(Edge edg)
        {
            double x1 = edg.From.X;
            double y1 = edg.From.Y;
            double x2 = edg.To.X;
            double y2 = edg.To.Y;
            ymax = y1 > y2 ? y2 : y1;
            xmin = ymax == y1 ? x2 : x1;
            m = (x2 - x1) / (y2 - y1);
            edge = edg;
        }
    }
}
