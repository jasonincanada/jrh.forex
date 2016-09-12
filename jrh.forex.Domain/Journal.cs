using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace jrh.forex.Domain
{
    /// <summary>
    /// Encapsulates the data in a forex journal format file (Journal.txt)
    /// </summary>
    public class Journal
    {
        public string HSTFiles { get; private set; }
        public List<StructurePoint> StructurePoints { get; private set; }
        public List<Channel> Channels { get; private set; }

        public static Journal FromFile(string file, Bars bars)
        {
            var journal = new Journal()
            {
                StructurePoints = new List<StructurePoint>(),
                Channels = new List<Channel>()
            };

            string[] slurp = File.ReadAllLines(file);

            foreach (var line in slurp)
            {
                if (line.Trim().StartsWith("#"))
                    continue;

                // Try parsing line as a HSTFiles directive
                Regex channel = new Regex(@"^\s*HSTFiles\s+(.+)$");
                var match = channel.Match(line);
                if (match.Success)
                {
                    journal.HSTFiles = match.Groups[1].Value.Trim();
                    continue;
                }

                // Try parsing line as a structure point
                StructurePoint sp = Parsers.ParseStructurePoint(line);
                if (sp != null)
                {
                    journal.StructurePoints.Add(sp);
                    continue;
                }

                // Try parsing line as a channel
                Channel ch = Parsers.ParseChannel(line, journal.StructurePoints, bars);
                if (ch != null)
                {                 
                    journal.Channels.Add(ch);
                    continue;
                }
            }

            return journal;
        }
    }
}
