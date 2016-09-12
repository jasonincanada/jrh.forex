using System;
using System.Collections.Generic;
using System.Linq;
using jrh.forex.MT4;

namespace jrh.forex.Domain
{
    public class Bars : IBarProvider
    {
        // Internal cache for bars
        private Dictionary<string, List<Bar>> _bars;
        private string _path;
                
        public static string HSTExtension { get { return ".hst"; } }

        public Bars()
        {
            _bars = new Dictionary<string, List<Bar>>();
        }

        public void SetSourceLocation(string path)
        {
            _path = path;
        }

        public static string HSTName(string symbol, string timeframe, string path, string extension)
        {
            var upper = symbol.ToUpper();
            string num = "15";

            if (timeframe == "M1") num = "1";
            if (timeframe == "M5") num = "5";
            if (timeframe == "M15") num = "15";
            if (timeframe == "H1") num = "60";
            if (timeframe == "H4") num = "240";
            if (timeframe == "D1") num = "1440";

            return path + upper + num + extension;
        }

        /// <summary>
        /// Read bars for this symbol's timeframe, then cache for subsequent calls
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="timeframe"></param>
        /// <returns></returns>
        public List<Bar> BarsForSymbol(string symbol, string timeframe)
        {
            if (string.IsNullOrEmpty(_path))
                throw new InvalidOperationException("Can't call IBarProvider.BarsForSymbol() before SetSourceLocation()");

            var key = symbol + " " + timeframe;

            if (_bars.ContainsKey(key))
                return _bars[key];

            string hstFile = HSTName(symbol, timeframe, _path, HSTExtension);

            var bars = MT4Util
                .GetHSTFile(hstFile)
                .Bars
                .Select(b => Bar.FromMT4Bar(b))
                .OrderBy(b => b.Date)
                .ToList();

            _bars.Add(key, bars);

            return bars;
        }

        /// <summary>
        /// Return statistics about the bars used
        /// </summary>
        /// <remarks>
        /// StatsForSymbol :: string -> string
        /// </remarks>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public string StatsForSymbol(string symbol, string timeframe)
        {
            string key = symbol + " " + timeframe;
            if (!_bars.ContainsKey(key))
                return "No symbol";

            var bars = _bars[key];

            var first = bars.OrderBy(b => b.Date).FirstOrDefault();
            var last = bars.OrderByDescending(b => b.Date).FirstOrDefault();
            var count = bars.Count();

            return string.Format("First: {0} Last: {1}: Count {2}", first, last, count);
        }

    }
}
