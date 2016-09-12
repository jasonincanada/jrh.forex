using System.Collections.Generic;

namespace jrh.forex.Domain
{
    public interface IBarProvider
    {
        List<Bar> BarsForSymbol(string symbol, string timeframe);
        void SetSourceLocation(string path);
    }
}
