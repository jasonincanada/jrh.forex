using System;

namespace jrh.forex.Domain
{
    /// <summary>
    /// An important structural point on the chart having its own Label
    /// </summary>
    public class StructurePoint
    {
        public string Label { get; private set; }
        public string Pair { get; private set; }
        public Point Point { get; private set; }

        public StructurePoint(string label, string pair, DateTime date, float price)
        {
            Label = label;
            Pair = pair;
            Point = new Point(date, price);
        }
    }
}
