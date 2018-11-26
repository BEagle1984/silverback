// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public class RequestPublisher : IRequestPublisher
    {
        private readonly IPublisher _publisher;

        public RequestPublisher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public IEnumerable<TResponse> Execute<TResponse>(IRequest<TResponse> requestMessage) => 
            _publisher.Publish<TResponse>(requestMessage);

        public Task<IEnumerable<TResponse>> ExecuteAsync<TResponse>(IRequest<TResponse> requestMessage) =>
            _publisher.PublishAsync<TResponse>(requestMessage);
    }
}