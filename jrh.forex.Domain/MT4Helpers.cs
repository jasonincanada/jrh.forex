using System;
using System.Collections.Generic;
using System.IO;
using jrh.forex.Domain;

namespace jrh.forex.MT4
{
    /// <remarks>
    /// From: https://forum.mql4.com/60455
    /// </remarks>
    public class HSTHeader
    {
        public int version;
        public string copyright; // 64 bytes
        public string symbol; // 12 bytes
        public int digits; // number of digits after the decimal point
        public DateTime timesign;   // time sign of database creation
        public DateTime last_sync;

        // reserved for future use
        public int[] unusued = new int[13];
    }

    public class HSTBar
    {
        public DateTime ctm;
        public double open;
        public double high;
        public double low;
        public double close;
        public long volume; // tick count
        public int spread;
        public long real_volume;

        public static HSTBar FromBytes(byte[] bytes)
        {
            HSTBar bar = new HSTBar();

            bar.ctm = new DateTime(1970, 1, 1)
                .AddSeconds(    BitConverter.ToInt64(bytes, 8 * 0));
            bar.open =          BitConverter.ToDouble(bytes, 8 * 1);
            bar.high =          BitConverter.ToDouble(bytes, 8 * 2);
            bar.low =           BitConverter.ToDouble(bytes, 8 * 3);
            bar.close =         BitConverter.ToDouble(bytes, 8 * 4);
            bar.volume =        BitConverter.ToInt64(bytes, 8 * 5);
            bar.spread =        BitConverter.ToInt32(bytes, 8 * 6);
            bar.real_volume =   BitConverter.ToInt64(bytes, 8 * 6 + 4);

            return bar;
        }

        public string Show()
        {
            return string.Format("{0} O-{1} H-{2} L-{3} C-{4}",
                ctm,
                open,
                high,
                low,
                close);
        }
    }

    public class MT4HistoryFile
    {
        public HSTHeader Header;
        public List<HSTBar> Bars;
    }

    public static class MT4Util
    {
        public const int HeaderSize = 148;
        public const int BarSize = 60;

        /// <summary>
        /// Read and parse the binary .HST file passed in
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static MT4HistoryFile GetHSTFile(string filename)
        {
            var info = new FileInfo(filename);
            long size = info.Length;
            long countBars = (size - HeaderSize) / BarSize;

            var file = new FileStream(filename, FileMode.Open);

            file.Seek(HeaderSize, SeekOrigin.Begin);
            // TODO: for now, don't do anything with the header

            MT4HistoryFile mt4 = new MT4HistoryFile();

            mt4.Bars = new List<HSTBar>();

            byte[] bar = new byte[BarSize];
            for (long i = 0; i < countBars; i++)
            {
                file.Read(bar, 0, BarSize);

                mt4.Bars.Add(HSTBar.FromBytes(bar));
            }

            return mt4;
        }

        /// <summary>
        /// Extends System.DateTime to add a ToMT4Date() method that returns the DateTime in the format recognized by MQL4's StringToTime() function.
        /// </summary>
        /// <remarks>
        /// ToMT4Date :: DateTime -> string
        /// </remarks>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string ToMT4Date(this DateTime dt)
        {
            return string.Format("{0:yyyy.MM.dd HH:mm}", dt);
        }

        /// <summary>
        /// Return the Channel as a line that our MT4 script can import for rendering on charts
        /// </summary>
        /// <remarks>
        /// ToMT4Format :: Channel -> string
        /// </remarks>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string ToMT4Format(Channel c)
        {
            if (c == null)
                throw new ArgumentNullException("c");

            return string.Format("C {0} {1} {2} {3} {4} {5} {6} {7} {8}",
                c.Symbol,
                c.Timeframe,
                c.Name.Replace(' ', '%'), // Spaces are the delimeters in this file and object names can have spaces
                c.Start.Date.ToMT4Date(),
                c.Start.Price,
                c.Cast.Date.ToMT4Date(),
                c.Cast.Price,
                c.Support.Date.ToMT4Date(),
                c.Support.Price);
        }
    }    
}
