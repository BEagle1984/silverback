namespace SilverbackShop.Catalog.Service.Dto
{
    public class UpdateProductRequest

    {
        public string DisplayName { get; set; }

        public string Description { get; set; }

        public decimal UnitPrice { get; set; }
    }
}