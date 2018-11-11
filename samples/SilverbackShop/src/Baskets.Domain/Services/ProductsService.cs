using System.Threading.Tasks;
using Common.Domain.Services;
using Silverback.Messaging.Subscribers;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;
using CatalogEvents = SilverbackShop.Catalog.Integration.Events;
using CatalogDto = SilverbackShop.Catalog.Integration.Dto;

namespace SilverbackShop.Baskets.Domain.Services
{
    public class ProductsService : IDomainService
    {
        private readonly IProductsRepository _repository;

        public ProductsService(IProductsRepository repository)
        {
            _repository = repository;
        }

        [Subscribe]
        public Task OnProductPublished(CatalogEvents.ProductPublishedEvent message)
            => AddOrUpdateProduct(message.Product);

        [Subscribe]
        public Task OnProductUpdated(CatalogEvents.ProductUpdatedEvent message)
            => AddOrUpdateProduct(message.Product);

        [Subscribe]
        public async Task OnProductDiscontinued(CatalogEvents.ProductDiscontinuedEvent message)
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
                product.Update(dto.DisplayName, dto.UnitPrice);
            }
            else
            {
                _repository.Add(Product.Create(dto.SKU, dto.DisplayName, dto.UnitPrice));
            }

            await _repository.UnitOfWork.SaveChangesAsync();
        }
    }
}