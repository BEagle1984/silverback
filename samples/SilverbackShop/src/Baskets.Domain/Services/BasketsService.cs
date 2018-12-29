using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Domain.Services;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;

namespace SilverbackShop.Baskets.Domain.Services
{
    public class BasketsService : IDomainService
    {
        private readonly IBasketsRepository _repository;

        public BasketsService(IBasketsRepository repository)
        {
            _repository = repository;
        }

        public async Task<Basket> GetOrCreateBasket(Guid userId)
        {
            var basket = await _repository.FindByUserAsync(userId);

            if (basket != null)
                return basket;

            basket = _repository.Add(Basket.Create(userId));
            await _repository.UnitOfWork.SaveChangesAsync();

            return basket;
        }
    }
}
