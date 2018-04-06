using System.Collections.Generic;

namespace Common.Data
{
    public static class CatalogData
    {
        public static IEnumerable<string> ProductIdentifiers
        {
            get
            {
                yield return "A1112";
                yield return "A1134";
                yield return "A1167";
                yield return "B2414";
                yield return "C0344";
            }
        }
    }
}