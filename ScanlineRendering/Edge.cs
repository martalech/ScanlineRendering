namespace ScanlineRendering
{
    public class Edge
    {
        public Vertex From { get; set; }
        public Vertex To { get; set; }

        public Edge(Vertex from, Vertex to)
        {
            From = from;
            To = to;
        }
    }
}
