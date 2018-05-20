using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;
using CatalogEvents = SilverbackShop.Catalog.Integration.Events;
using CatalogDto = SilverbackShop.Catalog.Integration.Dto;

namespace SilverbackShop.Baskets.Domain.Subscribers
{
    public class CatalogMultiSubscriber : MultiSubscriber
    {
        private readonly IProductsRepository _repository;

        public CatalogMultiSubscriber(IProductsRepository repository)
        {
            _repository = repository;
        }

        protected override void Configure(MultiSubscriberConfig config) => config
            .AddAsyncHandler<CatalogEvents.ProductPublishedEvent>(OnProductPublished)
            .AddAsyncHandler<CatalogEvents.ProductUpdatedEvent>(OnProductUpdated)
            .AddAsyncHandler<CatalogEvents.ProductDiscontinuedEvent>(OnProductDiscontinued);

        private Task OnProductPublished(CatalogEvents.ProductPublishedEvent message)
            => AddOrUpdateProduct(message.Product);

        private Task OnProductUpdated(CatalogEvents.ProductUpdatedEvent message)
            => AddOrUpdateProduct(message.Product);

        private async Task OnProductDiscontinued(CatalogEvents.ProductDiscontinuedEvent message)
        {
            var product = await _repository.FindBySkuAsync(message.SKU);

            if (product == null)
                return;

            _repository.Remove(product);
            await _repository.UnitOfWork.SaveChangesAsync();
        }

        private async Task AddOrUpdateProduct(CatalogDto.ProductDto dto)
        {
            var product = await _repository.FindBySkuAsync(dto.SKU);

            if (product != null)
            {
                product.UpdatePrice(dto.UnitPrice);
            }
            else
            {
                _repository.Add(Product.Create(dto.SKU, dto.UnitPrice));
            }

            await _repository.UnitOfWork.SaveChangesAsync();
        }
    }
}