// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing;

internal partial class ApplicationPublisher
{
    public void PublishEvent(IEvent eventMessage, bool throwIfUnhandled = false) =>
        _publisher.PublishEvent(eventMessage, throwIfUnhandled);

    public Task PublishEventAsync(IEvent eventMessage, CancellationToken cancellationToken = default) =>
        _publisher.PublishEventAsync(eventMessage, false, cancellationToken);

    public Task PublishEventAsync(
        IEvent eventMessage,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default) =>
        _publisher.PublishEventAsync(eventMessage, throwIfUnhandled, cancellationToken);
}
