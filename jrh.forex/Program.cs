using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using jrh.forex.Domain;
using jrh.forex.MT4;

namespace jrh.forex
{
    class Program
    {        
        static string BarsPath =   @"C:\Users\Jason\AppData\Roaming\MetaQuotes\Terminal\3212703ED955F10C7534BE8497B221F4\history\OANDA-GMT-5 Live\";
        static string OutputPath = @"C:\Users\Jason\AppData\Roaming\MetaQuotes\Terminal\3212703ED955F10C7534BE8497B221F4\MQL4\Files\";

        static void Main(string[] args)
        {
            Bars Bars = new Bars();
            Bars.SetSourceLocation(BarsPath);            

            var journal = Journal.FromFile(@"C:\Users\Jason\Documents\Visual Studio 2015\Projects\jrh.forex\jrh.forex\Journal.txt", Bars);

            // Resolve unspecified values in journal
            foreach (var channel in journal.Channels)
            {
                List<Bar> bars = Bars.BarsForSymbol(channel.Symbol, channel.Timeframe);

                var leftRight = channel.LeftRight;
                var ohlc = channel.OHLC;
                var fs = Parsers.GetChannelFunctions(leftRight, ohlc);
                var ohlcGetterForCast = fs.Item1;
                var ohlcGetterForSupport = fs.Item2;
                var comparerForCast = fs.Item3;
                var comparerForSupport = fs.Item4;

                var start = bars.Where(b => b.Date <= channel.Start.Date).LastOrDefault();

                if (channel.Cast == null)
                    channel.Cast = Channel.FindCastPoint(
                        start,
                        bars,
                        ohlcGetterForCast,
                        comparerForCast);

                if (channel.Support == null)
                    channel.Support = Channel.FindSupportPoint(
                        channel.Start,
                        Channel.Slope(channel),
                        bars,
                        ohlcGetterForSupport,
                        comparerForSupport
                        );

                Console.WriteLine("Channel: {0}", channel);
                Console.WriteLine("Bar stats: {0}", Bars.StatsForSymbol(channel.Symbol, channel.Timeframe));
            }

            // Output the object transfer file for MT4 script consumption
            using (var sw = new StreamWriter(OutputPath + "jrh.forex.txt"))
            {
                foreach (var channel in journal.Channels)
                    sw.WriteLine("{0}", MT4Util.ToMT4Format(channel));

                sw.Close();
            }
        }
    }
}
