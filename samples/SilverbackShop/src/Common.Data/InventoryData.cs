using System;
using System.Collections.Generic;

namespace SilverbackShop.Common.Data
{
    public static class InventoryData
    {
        public static IEnumerable<Tuple<string, int>> InitialStock
        {
            get
            {
                var rnd = new Random((int)DateTime.Now.Ticks);
                foreach (var id in CatalogData.ProductIdentifiers)
                {
                    yield return new Tuple<string, int>(id, rnd.Next(0, 10));
                }
            }
        }
    }
}
