// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    internal class StreamPublisher : IStreamPublisher
    {
        private readonly IPublisher _publisher;

        public StreamPublisher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public IReadOnlyCollection<Task> Publish(IMessageStreamProvider streamProvider) =>
            _publisher.Publish<Task>(streamProvider);

        public async Task<IReadOnlyCollection<Task>> PublishAsync(IMessageStreamProvider streamProvider) =>
            await _publisher.PublishAsync<Task>(streamProvider).ConfigureAwait(false);
    }
}
