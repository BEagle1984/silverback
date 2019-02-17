// Copyright (c) 2018-2019 Sergio Aquilini
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

        public TResponse Send<TResponse>(IRequest<TResponse> requestMessage) =>
            _publisher.Publish<TResponse>(requestMessage).SingleOrDefault();

        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> requestMessage) =>
            (await _publisher.PublishAsync<TResponse>(requestMessage)).SingleOrDefault();
    }
}