using System;
using jrh.forex.MT4;

namespace jrh.forex.Domain
{
    public class Bar
    {
        public static DateTime MinDate = DateTime.Parse("1/1/2000");

        public DateTime Date {  get; private set; }
        public float High { get; private set; }
        public float Low { get; private set; }
        public float Open { get; private set; }
        public float Close { get; private set; }

        public Bar(DateTime date, float high, float low, float open, float close)
        {
            if (date < MinDate)
                throw new ArgumentOutOfRangeException("Date is too low: " + date.ToString());

            Date = date;
            High = high;
            Low = low;
            Open = open;
            Close = close;
        }

        public override string ToString()
        {
            return string.Format("{0} O-{1} H-{2} L-{3} C-{4}", Date, Open, High, Low, Close);
        }

        public static Bar FromMT4Bar(HSTBar b)
        {
            return new Bar(
                b.ctm,
                Convert.ToSingle(b.high),
                Convert.ToSingle(b.low),
                Convert.ToSingle(b.open),
                Convert.ToSingle(b.close));
        }
    }

}
