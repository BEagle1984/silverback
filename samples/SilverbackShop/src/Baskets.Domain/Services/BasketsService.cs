using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Baskets.Domain.Model.BasketAggregate;
using Baskets.Domain.Repositories;

namespace Baskets.Domain.Services
{
    public class BasketsService
    {
        private readonly IBasketsRepository _repository;

        public BasketsService(IBasketsRepository repository)
        {
            _repository = repository;
        }

        public async Task<Basket> GetBasket(Guid userId)
        {
            var basket = _repository.FindUserBasket(userId);

            if (basket != null)
                return basket;

            basket = _repository.Add(Basket.Create(userId));
            await _repository.UnitOfWork.SaveChangesAsync();

            return basket;
        }
    }
}
