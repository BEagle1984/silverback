using System;
using System.Linq;
using System.Threading.Tasks;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;

namespace SilverbackShop.Baskets.Domain.Services
{
    public class BasketsService
    {
        private readonly IBasketsRepository _repository;

        public BasketsService(IBasketsUnitOfWork unitOfWork)
        {
            _repository = unitOfWork.Baskets;
        }

        public async Task<Basket> GetUserBasket(Guid userId)
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
