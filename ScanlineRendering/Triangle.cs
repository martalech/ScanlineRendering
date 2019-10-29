﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
                canvas.Children.Add(new Line()
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    X1 = edge.From.X,
                    Y1 = edge.From.Y,
                    X2 = edge.To.X,
                    Y2 = edge.To.Y
                });
        }
    }
}