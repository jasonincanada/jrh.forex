using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using jrh.forex.Domain;

namespace jrh.forex.Tests
{
    [TestClass]
    public class TestMT4Util
    {
        [TestMethod]
        public void Test_ChannelToMT4()
        {
            Channel c = new Channel
            {
                Name = "AUD Hourly Channel",
                Symbol = "AUDUSD",
                Timeframe = Timeframe.M15,
                Start = new Point(DateTime.Parse("9/8/2016 8:40"), 0.77315f),
                Cast = new Point(DateTime.Parse("9/9/2016 2:00"), 0.76525f),
                Support = new Point(DateTime.Parse("9/8/2016 12:00"), 0.76441f)
            };

            // Dates in "yyyy.mm.dd hh:mi" format
            Assert.AreEqual(
                "C AUDUSD M15 AUD%Hourly%Channel 2016.09.08 08:40 0.77315 2016.09.09 02:00 0.76525 2016.09.08 12:00 0.76441", 
                MT4.MT4Util.ToMT4Format(c));
        }
    }
}
