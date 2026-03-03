// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Publishing;

internal partial class ApplicationPublisher : IApplicationPublisher
{
    private readonly IPublisher _publisher;

    public ApplicationPublisher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public ISilverbackContext Context => _publisher.Context;

    public void Publish(object message, bool throwIfUnhandled = false) =>
        _publisher.Publish(message, throwIfUnhandled);

    public IReadOnlyCollection<TResult> Publish<TResult>(object message, bool throwIfUnhandled = false) =>
        _publisher.Publish<TResult>(message, throwIfUnhandled);

    public Task PublishAsync(object message, CancellationToken cancellationToken = default) =>
        _publisher.PublishAsync(message, cancellationToken);

    public Task PublishAsync(object message, bool throwIfUnhandled, CancellationToken cancellationToken = default) =>
        _publisher.PublishAsync(message, throwIfUnhandled, cancellationToken);

    public Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(object message, CancellationToken cancellationToken = default) =>
        _publisher.PublishAsync<TResult>(message, cancellationToken);

    public Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(
        object message,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default) =>
        _publisher.PublishAsync<TResult>(message, throwIfUnhandled, cancellationToken);
}
