using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickLaunch.Core.Models
{
    public static class ScoringRules
    {
        public static readonly Dictionary<string, int> ExtensionScores = new()
        {
            [".exe"] = 100,
            [".lnk"] = 90,
            [".pdf"] = 80,
            [".docx"] = 80,
            [".txt"] = 40,

            [".meta"] = -1000,
            [".tmp"] = -1000,
            [".cache"] = -1000
        };
    }
}
