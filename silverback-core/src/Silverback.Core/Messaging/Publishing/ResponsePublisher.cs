// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public class ResponsePublisher<TResponse> : IResponsePublisher<TResponse>
        where TResponse : IResponse
    {
        private readonly IPublisher _publisher;

        public ResponsePublisher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public void Reply(TResponse responseMessage) => _publisher.Publish(responseMessage);

        public Task ReplyAsync(TResponse responseMessage) => _publisher.PublishAsync(responseMessage);
    }
}