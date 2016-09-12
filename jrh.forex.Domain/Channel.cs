using System;
using System.Collections.Generic;
using System.Linq;

namespace jrh.forex.Domain
{
    /// <summary>
    /// An equidistant channel with Start, Cast, and Support points. These are identical to the 3 parameters that MT4
    /// uses for its equidistant channels.
    /// </summary>
    public class Channel
    {
        public string Name;
        public string Symbol;
        public string Timeframe;
        public Handed LeftRight;
        public OHLC OHLC;

        /// <summary>
        /// The point where the channel statrs
        /// </summary>
        public Point Start;

        /// <summary>
        /// The point that encapsulates the "upper" part of the channel
        /// </summary>
        public Point Cast;

        /// <summary>
        /// The point that encapsulates the "bottom" part of the channel, uses slope between Start and Cast
        /// </summary>
        public Point Support;
           
        public override string ToString()
        {
            return string.Format("|| {0} {1} {2} {3} {4} {5}", Name, Symbol, Timeframe, Start, Cast, Support);
        }

        #region static methods
        
        /// <remarks>
        /// FindCastPoint :: Bar -> [Bar] -> (Bar -> float) -> (Bar -> Bar -> float -> Bar) -> Point
        /// </remarks>
        /// <param name="start"></param>
        /// <param name="bars"></param>
        /// <param name="ohlcGetter">Function that gets the O/H/L/C from the passed bar</param>
        /// <param name="comparer">Compares two bars and returns the one more suited to being the cast point for this channel</param>
        /// <returns></returns>
        public static Point FindCastPoint(Bar start, List<Bar> bars, Func<Bar, float> ohlcGetter, Func<Bar, Bar, Func<Bar, float>, Bar, Bar> comparer)
        {
            var afterStart = bars
                .Where(b => b.Date > start.Date)
                .ToList();

            // Need at least one bar
            if (afterStart.Count == 0)
                return new Point(start.Date, ohlcGetter(start));

            float price = ohlcGetter(start);

            var bar = afterStart
                .Aggregate(afterStart[0], (a, b) => comparer(a, b, ohlcGetter, start));

            return new Point(bar.Date, ohlcGetter(bar));
        }

        /// <summary>
        /// Given a starting point for a channel and its cast, find the line with the same slope that encompasses all the candles
        /// </summary>
        /// <remarks>
        /// FindSupportPoint :: Point -> float -> [Bar] -> (Bar -> float> -> (Bar -> Bar -> float -> Bar)
        /// </remarks>
        /// <param name="start">Channel starting point</param>
        /// <param name="slope">Slope of channel</param>
        /// <param name="bars">All bars for this timeframe</param>
        /// <param name="ohlcGetter">Function that gets a O/H/L/C value from a Bar</param>
        /// <param name="comparerForSupport">Compares two bars and returns the one more suited to be the support point for this channel</param>
        /// <returns></returns>
        public static Point FindSupportPoint(Point start, float slope, List<Bar> bars, Func<Bar, float> ohlcGetter, Func<Bar, Bar, float, Bar> comparerForSupport)
        {
            var afterStart = bars
                .Where(b => b.Date >= start.Date)
                .ToList();

            if (afterStart.Count == 0)
                return start;
            
            var bar = afterStart
                .Aggregate(afterStart[0], (a, b) => comparerForSupport(a, b, slope));

            return new Point(bar.Date, ohlcGetter(bar));
        }

        /// <summary>
        /// Calculates the slope between two price points
        /// </summary>
        /// <remarks>
        /// Slope :: Point -> Point -> float
        /// </remarks>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float Slope(Point a, Point b)
        {
            float deltaPrice = a.Price - b.Price;
            double deltaTime = (a.Date - b.Date).TotalSeconds;

            if (deltaTime == 0)
                return 0;
            else 
                return Convert.ToSingle(deltaPrice / deltaTime);
        }

        public static float Slope(Bar a, Bar b, Func<Bar, float> f)
        {
            return Slope(ToPoint(a, f), ToPoint(b, f));
        }
        
        public static float Slope(Channel channel)
        {
            return Slope(channel.Start, channel.Cast);
        }

        public static float Slope(Bar a, Point b, Func<Bar, float> f)
        {
            return Slope(ToPoint(a, f), b);
        }

        /// <summary>
        /// Convert the O/H/L/C of a bar to its corresponding Point
        /// </summary>
        /// <remarks>
        /// ToPoint :: Bar -> (Bar -> float) -> Point
        /// </remarks>
        /// <param name="b"></param>
        /// <param name="f">Take the Open, High, Low, or Close</param>
        /// <returns></returns>
        public static Point ToPoint(Bar b, Func<Bar, float> f)
        {
            return new Point(b.Date, f(b));
        }
             
        #endregion
    }
}
