using System.Collections.Generic;

namespace AutoTrader.Menu
{
    /// <summary>Works out where a mod option goes in a menu that other code already filled.</summary>
    public static class MenuPlacement
    {
        /// <summary>
        /// The insert index lying directly behind the first option named <paramref name="anchorOptionId"/>,
        /// or, if the anchor is not in the menu, the index of the first option named
        /// <paramref name="fallbackBeforeOptionId"/> (so the new option lands in front of it),
        /// or -1 (append at the end) if that one is missing too or not given.
        /// </summary>
        public static int IndexAfter(IEnumerable<string> optionIds, string anchorOptionId, string fallbackBeforeOptionId = null)
        {
            int position = 0;
            int fallbackIndex = -1;
            foreach (string id in optionIds)
            {
                if (id == anchorOptionId)
                {
                    return position + 1;
                }

                if (fallbackIndex < 0 && fallbackBeforeOptionId != null && id == fallbackBeforeOptionId)
                {
                    fallbackIndex = position;
                }

                position++;
            }

            return fallbackIndex;
        }
    }
}
