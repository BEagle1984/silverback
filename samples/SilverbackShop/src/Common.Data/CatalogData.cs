using System.Collections.Generic;

namespace SilverbackShop.Common.Data
{
    public static class CatalogData
    {
        public static IEnumerable<string> ProductIdentifiers
        {
            get
            {
                yield return "A1";
                yield return "A2";
                yield return "A3";
                yield return "B2";
                yield return "C3";
            }
        }
    }
}