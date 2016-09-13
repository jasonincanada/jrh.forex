using System;
using System.Linq;

namespace jrh.forex.Domain
{
    public static class Charting
    {
        public static int TimeframeAsMinutes(Timeframe timeframe)
        {
            switch (timeframe)
            {
                case Timeframe.W1: return 1440 * 7;
                case Timeframe.D1: return 1440;
                case Timeframe.H4: return 240;
                case Timeframe.H1: return 60;
                case Timeframe.M30: return 30;
                case Timeframe.M15: return 15;
                case Timeframe.M5: return 5;
                case Timeframe.M1: return 1;

                default:
                    throw new NotImplementedException("Unknown timeframe [" + timeframe + "]");
            }
        }

        public static Timeframe StringToTimeframe(string timeframe)
        {
            switch (timeframe)
            {
                case "M1": return Timeframe.M1;
                case "M5": return Timeframe.M5;
                case "M15": return Timeframe.M15;
                case "M30": return Timeframe.M30;
                case "H1": return Timeframe.H1;
                case "H4": return Timeframe.H4;
                case "D1": return Timeframe.D1;
                case "W1": return Timeframe.W1;

                default: throw new ArgumentOutOfRangeException("Unknown timeframe [" + timeframe + "]");
            }
        }

        /// <summary>
        /// Check 15-min bars to get a more precise time this low or high occurred
        /// </summary>
        /// <param name="point"></param>
        /// <param name="symbol"></param>
        /// <param name="timeframe"></param>
        /// <param name="bars"></param>
        /// <param name="ohlc"></param>
        /// <returns>A more precise date or, if none, the date in the original passed point</returns>
        public static DateTime GetMorePreciseTime(Point point, string symbol, Timeframe timeframe, Bars bars, OHLC ohlc, Func<Bar, float> ohlcGetter)
        {
            int minutes = Charting.TimeframeAsMinutes(timeframe);

            // Only bother with M30 and above
            if (minutes <= 15)
                return point.Date;

            // Get the 15-min bars that span a bar of this timeframe
            var bs = bars
                    .BarsForSymbol(symbol, Timeframe.M15)
                    .Where(b => b.Date >= point.Date
                                && b.Date < point.Date.AddMinutes(minutes))
                    .ToList();

            if (bs.Count == 0)
                return point.Date;

            var match = bs
                .Aggregate((a, b) => ohlcGetter(b) == point.Price ? b : a);

            return match.Date;
        }
    }
}
