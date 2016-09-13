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

        public static string HSTName(string symbol, Timeframe timeframe, string path, string extension)
        {
            var upper = symbol.ToUpper();

            int minutes = Charting.TimeframeAsMinutes(timeframe);

            return path + upper + minutes + extension;
        }

        /// <summary>
        /// Read bars for this symbol's timeframe, then cache for subsequent calls
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="timeframe"></param>
        /// <returns></returns>
        public List<Bar> BarsForSymbol(string symbol, Timeframe timeframe)
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

        public void Flush()
        {
            _bars.Clear();
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
