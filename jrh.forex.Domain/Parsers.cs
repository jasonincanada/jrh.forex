using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace jrh.forex.Domain
{
    public static class Parsers
    {
        /// <summary>
        /// Return either Left or Right if passed 'l' or 'r'
        /// </summary>
        /// <remarks>
        /// ToHanded :: char -> Handed
        /// </remarks>
        /// <param name="handed"></param>
        /// <returns></returns>
        public static Handed ToHanded(char handed)
        {
            switch (handed)
            {
                case 'l':
                case 'L':
                    return Handed.Left;

                case 'r':
                case 'R':
                    return Handed.Right;
            }

            throw new ArgumentOutOfRangeException("Unknown handedness character " + handed);
        }

        /// <summary>
        /// Return either Open, High, Low, or Close if passed 'o', 'h', 'l', or 'c'
        /// </summary>
        /// <remarks>
        /// ToOHLC :: char -> OHLC
        /// </remarks>
        /// <param name="handed"></param>
        /// <returns></returns>
        public static OHLC ToOHLC(char ohlc)
        {
            switch (ohlc)
            {
                case 'o':
                case 'O':
                    return OHLC.Open;

                case 'h':
                case 'H':
                    return OHLC.High;

                case 'l':
                case 'L':
                    return OHLC.Low;

                case 'c':
                case 'C':
                    return OHLC.Close;
            }

            throw new ArgumentOutOfRangeException("Unknown enum value for OHLC");
        }

        /// <summary>
        /// Return Low if passed High and vice versa
        /// </summary>
        /// <remarks>
        /// OppositeOHLC :: OHLC -> OHLC
        /// </remarks>
        /// <param name="ohlc"></param>
        /// <returns></returns>
        public static OHLC OppositeOHLC(OHLC ohlc)
        {
            switch (ohlc)
            {
                case OHLC.High:
                    return OHLC.Low;

                case OHLC.Low:
                    return OHLC.High;
            }

            throw new ArgumentOutOfRangeException("Unknown enum value for OHLC");
        }

        /// <summary>
        /// Parse a string from the journal file format into a StructurePoint
        /// </summary>
        /// <remarks>
        /// ParseStructurePoint :: string -> StructureLine
        /// </remarks>
        /// <param name="line"></param>
        /// <returns></returns>
        public static StructurePoint ParseStructurePoint(string line)
        {
            if (!Parsing.Forex.IsStructurePoint(line))
                return null;

            var sp = Parsing.Forex.ParseStructurePoint(line);

            return new StructurePoint(
                sp.Label,
                sp.Symbol,
                Convert.ToDateTime(sp.Date),
                Convert.ToSingle(sp.Price)
            );
        }

        /// <summary>
        /// Parse a string from the journal file format into a channel
        /// </summary>
        /// <remarks>
        /// ParseChannel :: string -> [StructurePoint] -> IBarProvider -> Channel
        /// </remarks>
        /// <param name="line"></param>
        /// <param name="points"></param>
        /// <param name="bars"></param>
        /// <returns></returns>
        public static Channel ParseChannel(string line, IEnumerable<StructurePoint> points, IBarProvider bars)
        {
            if (!Parsing.Forex.IsChannel(line))
                return null;

            var ch = Parsing.Forex.ParseChannel(line);
            
            string timeframe = ch.Timeframe;
            string symbol = ch.Symbol;
            Handed handed = ToHanded(ch.LeftRight[0]);
            OHLC ohlc = ToOHLC(ch.OHLC[0]);

            var barsSymbol = bars.BarsForSymbol(symbol, timeframe);
            Point start = ParsePoint(ch.Start, points, barsSymbol, ohlc);

            if (start == null)
                throw new NotImplementedException("Channel has no starting point");

            // These may be null if empty, the calling code will notice and locate the cast/support points
            Point cast = ParsePoint(ch.Cast, points, barsSymbol, ohlc);
            Point support = ParsePoint(ch.Support, points, barsSymbol, OppositeOHLC(ohlc));

            return new Channel
            {
                Name = ch.Label,
                Symbol = ch.Symbol,
                Timeframe = ch.Timeframe,
                Start = start,
                Cast = cast,
                Support = support,
                LeftRight = handed,
                OHLC = ohlc
            };
        }
                
        /// <summary>
        /// Return functions used to determine a channel's cast and support points
        /// </summary>
        /// <param name="leftRight"></param>
        /// <param name="ohlc"></param>
        /// <returns></returns>
        public static Tuple<
                Func<Bar, float>,
                Func<Bar, float>,
                Func<Bar, Bar, Func<Bar, float>, Bar, Bar>,
                Func<Bar, Bar, float, Bar>                
            >
            GetChannelFunctions(Handed leftRight, OHLC ohlc)
        {
            Func<Bar, float> ohlcGetterForCast;
            Func<Bar, float> ohlcGetterForSupport;
            Func<Bar, Bar, Func<Bar, float>, Bar, Bar> comparerForCast;
            Func<Bar, Bar, float, Bar> comparerForSupport;

            if (ohlc == OHLC.Low)
            {
                ohlcGetterForCast = x => x.Low;
                ohlcGetterForSupport = x => x.High;
                                
                comparerForCast = (a, b, f, start)
                    => Channel.Slope(a, start, f) 
                       < Channel.Slope(b, start, f)
                        ? a 
                        : b;
            } 
            else
            {
                ohlcGetterForCast = x => x.High;
                ohlcGetterForSupport = x => x.Low;

                comparerForCast = (a, b, f, start)
                    => Channel.Slope(a, start, f)
                       > Channel.Slope(b, start, f)
                        ? a
                        : b;
            }
            
            if (leftRight == Handed.Left)
            {
                comparerForSupport =
                    (bar_a, bar_b, slope) =>
                        Channel.Slope(bar_a, bar_b, ohlcGetterForSupport) 
                        < slope
                            ? bar_a
                            : bar_b;
            } 
            else
            {
                comparerForSupport =
                    (bar_a, bar_b, slope) =>
                        Channel.Slope(bar_a, bar_b, ohlcGetterForSupport)
                        > slope
                            ? bar_a
                            : bar_b;
            }

            return new Tuple<
                    Func<Bar, float>, 
                    Func<Bar, float>, 
                    Func<Bar, Bar, Func<Bar, float>, Bar, Bar>,
                    Func<Bar, Bar, float, Bar>>
            (
                ohlcGetterForCast,
                ohlcGetterForSupport,
                comparerForCast,
                comparerForSupport
            );
        }

        /// <summary>
        /// Takes an argument to a channel directive in the Journal file, then resolves and returns the corresponding Point
        /// </summary>
        /// <remarks>
        /// ParsePoint :: string -> [StructurePoint] -> [Bar] -> OHLC -> Point
        /// </remarks>
        /// <param name="label"></param>
        /// <param name="points"></param>
        /// <param name="bars">The List of Bars for the timeframe in question</param>
        /// <param name="ohlc">The Open, High, Low, or Close</param>
        /// <returns></returns>
        private static Point ParsePoint(string label, IEnumerable<StructurePoint> points, List<Bar> bars, OHLC ohlc)
        {
            // Test for literal <date time price>, eg: <09/07/2016 10:00 1.29128>
            Regex regex = new Regex(@"^<(.+) ([\d\.]+)>$");
            var match = regex.Match(label);
            if (match.Success)
                return new Point(
                    Convert.ToDateTime(match.Groups[1].Value),
                    Convert.ToSingle(match.Groups[2].Value));

            // Test for just {date time}, eg: {08/08/2016 00:00}
            regex = new Regex(@"^{(.+)}$");
            match = regex.Match(label);

            if (match.Success)
            {
                DateTime date = Convert.ToDateTime(match.Groups[1].Value);                                              

                // If we were passed the time 09:34 but are working with hourly bars, this will match the bar at 09:00
                var closestBar = bars                    
                    .Where(b => b.Date <= date)
                    .LastOrDefault();
                               
                float price = PriceFromBar(closestBar, ohlc);

                return new Point(
                    closestBar.Date,
                    price);
            }

            // Search defined structure points for one by this label
            var structurePoint = points
                .Where(p => p.Label == label)
                .FirstOrDefault();

            if (structurePoint == null)
                return null;

            return structurePoint.Point;
        }

        /// <summary>
        /// Return a function that takes a Bar and returns the Open, High, Low, or Close
        /// </summary>
        /// <remarks>
        /// GetOHLCGetter :: OHLC -> (Bar -> float)
        /// </remarks>
        /// <param name="ohlc">Open, High, Low, or Close</param>
        /// <returns></returns>
        public static Func<Bar, float> GetOHLCGetter(OHLC ohlc)
        {
            switch (ohlc)
            {
                case OHLC.Open:  return (x => x.Open);
                case OHLC.High:  return (x => x.High);
                case OHLC.Low:   return (x => x.Low);
                case OHLC.Close: return (x => x.Close);                
            }

            throw new ArgumentOutOfRangeException("Unknown enum value for OHLC");
        }

        /// <summary>
        /// Returns the Open, High, Low, or Close of the passed bar
        /// </summary>
        /// <remarks>
        /// PriceFromBar :: Bar -> OHLC -> float
        /// </remarks>
        /// <param name="bar"></param>
        /// <param name="ohlc"></param>
        /// <returns></returns>
        public static float PriceFromBar(Bar bar, OHLC ohlc)
        {
            switch (ohlc)
            {
                case OHLC.Open:     return bar.Open;
                case OHLC.High:     return bar.High;
                case OHLC.Low:      return bar.Low;
                case OHLC.Close:    return bar.Close;                                    
            }

            throw new ArgumentOutOfRangeException("Unknown enum value for OHLC");
        }        
    }
}
