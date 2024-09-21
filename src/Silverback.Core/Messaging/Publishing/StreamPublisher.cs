// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing;

internal sealed class StreamPublisher : IStreamPublisher
{
    private readonly IPublisher _publisher;

    public StreamPublisher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public IReadOnlyCollection<Task> Publish(IMessageStreamProvider streamProvider) =>
        _publisher.Publish<Task>(streamProvider);

    public Task<IReadOnlyCollection<Task>> PublishAsync(IMessageStreamProvider streamProvider, CancellationToken cancellationToken = default) =>
        _publisher.PublishAsync<Task>(streamProvider, cancellationToken);
}
