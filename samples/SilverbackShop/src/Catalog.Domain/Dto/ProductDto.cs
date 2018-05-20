using System;
using System.Collections.Generic;
using System.Text;
using SilverbackShop.Catalog.Domain.Model;

namespace SilverbackShop.Catalog.Domain.Dto
{
    public class ProductDto
    {
        public string SKU { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public decimal UnitPrice { get; set; }

        public bool IsPublished { get; set; }

        public bool IsDiscontinued { get; set; }
    }
}
