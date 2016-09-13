using System.Collections.Generic;

namespace jrh.forex.Domain
{
    public interface IBarProvider
    {
        List<Bar> BarsForSymbol(string symbol, Timeframe timeframe);
        void SetSourceLocation(string path);
    }
}
