using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using NDesk.Options;
using jrh.forex.Domain;
using jrh.forex.MT4;

namespace jrh.forex
{
    class Program
    {
        // See App.config comments for descriptions of these settings
        static string BarsPath = ConfigurationManager.AppSettings["BarsPath"];
        static string OutputPath = ConfigurationManager.AppSettings["OutputPath"];
        static string JournalFile = ConfigurationManager.AppSettings["JournalFile"];
        static string DropFile = ConfigurationManager.AppSettings["DropFile"];
        static int WatchDelaySeconds = Convert.ToInt32(ConfigurationManager.AppSettings["WatchDelaySeconds"]);

        static void Main(string[] args)
        {
            string journalFile = string.Empty;
            bool watch = false;

            Bars Bars = new Bars();
            Bars.SetSourceLocation(BarsPath);

            var options = new OptionSet
            {
                { "journal=", x => journalFile = x },
                { "watch", x => watch = (x != null) }
            };

            options.Parse(args);

            // If not specified on the command line, use the JournalFile from App.config
            if (string.IsNullOrEmpty(journalFile))
                journalFile = JournalFile;

            if (!string.IsNullOrEmpty(journalFile))
            {
                ProcessJournal(journalFile, Bars);

                // By "watch" we mean just loop once a minute until ^C
                while (watch)
                {
                    Console.WriteLine("Waiting {0} seconds", WatchDelaySeconds);
                    System.Threading.Thread.Sleep(1000 * WatchDelaySeconds);

                    // Cause a re-read of the history files
                    Bars.Flush();
                    ProcessJournal(journalFile, Bars);
                }
            }
        }

        private static void ProcessJournal(string journalFile, Bars Bars)
        {
            var journal = Journal.FromFile(journalFile, Bars);

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

                // Look for a more precise time by looking at smaller timeframe for the high/low within this "bigger" bar
                DateTime preciseTime = Charting.GetMorePreciseTime(channel.Cast, channel.Symbol, channel.Timeframe, Bars, ohlc, ohlcGetterForCast);

                channel.Cast = new Point(preciseTime, channel.Cast.Price);

                if (channel.Support == null)
                    channel.Support = Channel.FindSupportPoint(
                        channel.Start,
                        Channel.Slope(channel),
                        bars,
                        ohlcGetterForSupport,
                        comparerForSupport
                        );

                Console.WriteLine("Channel: {0}", channel);
                Console.WriteLine("Bar stats: {0}", Bars.StatsForSymbol(channel.Symbol, channel.Timeframe.ToString()));
            }

            // Output the object transfer file for MT4 script consumption
            using (var sw = new StreamWriter(OutputPath + DropFile))
            {
                foreach (var channel in journal.Channels)
                    sw.WriteLine("{0}", MT4Util.ToMT4Format(channel));

                sw.Close();
            }
        }
    }
}
