using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoTraderSpeed.Scenarios
{
    /// <summary>Readable form of a <c>SnapshotDiff.CountDelta</c> result for check output.</summary>
    internal static class DeltaText
    {
        /// <summary>"{key:+n, key:-m}" sorted by key (ordinal); "{}" when nothing changed.</summary>
        public static string Describe(IReadOnlyDictionary<string, int> delta)
        {
            if (delta.Count == 0)
            {
                return "{}";
            }

            return "{" + string.Join(", ", delta.OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => p.Key + ":" + (p.Value > 0 ? "+" : string.Empty) + p.Value)) + "}";
        }
    }
}
