using System;

namespace jrh.forex.Domain
{
    public class Point
    {
        public DateTime Date { get; private set; }
        public float Price { get; private set; }

        public Point(DateTime date, float price)
        {
            if (date < DateTime.Parse("1/1/2005"))
                throw new ArgumentOutOfRangeException("Date can't be earlier than year 2005 and was " + date.ToString() + ")");

            if (!(price > 0f))
                throw new ArgumentOutOfRangeException("Price must be positive");

            Date = date;
            Price = price;
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}", Date, Price);
        }
    }
}
