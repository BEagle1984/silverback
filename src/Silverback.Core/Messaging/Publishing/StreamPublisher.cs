// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
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

    // The null results (returned by a non-stream subscriber that happens to match the stream provider and declares a Task result type) are
    // discarded: only the processing tasks of the stream subscribers are relevant
    public IReadOnlyCollection<Task> Publish(IMessageStreamProvider streamProvider) =>
        [.. _publisher.Publish<Task?>(streamProvider).OfType<Task>()];

    public async Task<IReadOnlyCollection<Task>> PublishAsync(IMessageStreamProvider streamProvider, CancellationToken cancellationToken = default) =>
        [.. (await _publisher.PublishAsync<Task?>(streamProvider, cancellationToken).ConfigureAwait(false)).OfType<Task>()];
}
