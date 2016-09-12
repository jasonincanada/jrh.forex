using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using jrh.forex.Domain;

namespace jrh.forex.Tests
{
    [TestClass]
    public class TestParsers
    {
        static string BarsPath = @"C:\Users\Jason\AppData\Roaming\MetaQuotes\Terminal\3212703ED955F10C7534BE8497B221F4\history\OANDA-GMT-5 Live\";

        [TestMethod]
        public void Test_StructurePoint_FromFile_Valid()
        {
            string line = "[High-1] AUDUSD 09/08/2016 08:40 0.77315";

            StructurePoint sp = Parsers.ParseStructurePoint(line);

            Assert.IsNotNull(sp);

            Assert.AreEqual(DateTime.Parse("9/8/2016 8:40"), sp.Point.Date);
            Assert.AreEqual("High-1", sp.Label);
            Assert.AreEqual("AUDUSD", sp.Pair);
            Assert.AreEqual(0.77315f, sp.Point.Price);
        }

        [TestMethod]
        public void Test_StructurePoint_FromFile_Invalid()
        {
            string line = "giggity";

            StructurePoint sp = Parsers.ParseStructurePoint(line);

            Assert.IsNull(sp);
        }

        public enum WhichPoints
        {
            StartOnly,
            StartCastSupport
        }

        [TestMethod]
        public void Test_Channel_Valid()
        {
            List<StructurePoint> points = GetStructurePoints(WhichPoints.StartOnly);

            string line = "|AUD Hourly| AUDUSD M7 L L [High-1] [cast] [support]";

            var bars = new Bars();

            bars.SetSourceLocation(BarsPath);

            Channel ch = Parsers.ParseChannel(line, points, bars);
            Assert.IsNotNull(ch);
            Assert.AreEqual("AUD Hourly", ch.Name);
            Assert.AreEqual("AUDUSD", ch.Symbol);
            Assert.AreEqual("M15", ch.Timeframe);
            Assert.AreEqual(0.77315, ch.Start.Price, 0.00001);
            Assert.AreEqual(DateTime.Parse("9/8/2016 8:40"), ch.Start.Date);
            Assert.AreEqual(OHLC.Low, ch.OHLC);
            Assert.AreEqual(Handed.Left, ch.LeftRight);

            // These are not known points
            Assert.IsNull(ch.Cast);
            Assert.IsNull(ch.Support);

            // Add those points and try again
            points = GetStructurePoints(WhichPoints.StartCastSupport);            

            ch = Parsers.ParseChannel(line, points, bars);
            Assert.IsNotNull(ch);
            Assert.AreEqual("AUD Hourly", ch.Name);
            Assert.AreEqual("AUDUSD", ch.Symbol);
            Assert.AreEqual(0.77315, ch.Start.Price, 0.00001);
            Assert.AreEqual(0.8888f, ch.Cast.Price, 0.00001);
            Assert.AreEqual(0.9999f, ch.Support.Price, 0.00001);
        }

        // Helper method for tests
        private List<StructurePoint> GetStructurePoints(WhichPoints which)
        {
            var list = new List<StructurePoint>()
            {
                new StructurePoint("High-1", "AUDUSD", DateTime.Parse("9/8/2016 8:40"), 0.77315f)
            };

            if (which == WhichPoints.StartCastSupport)
            {
                list.Add(new StructurePoint("cast", "AUDUSD", DateTime.Parse("9/9/2016 10:00"), .8888f));
                list.Add(new StructurePoint("support", "AUDUSD", DateTime.Parse("9/10/2016 10:00"), .9999f));
            }

            return list;
        }
    }
}
