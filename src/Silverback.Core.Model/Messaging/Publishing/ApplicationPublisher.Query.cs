// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing;

internal partial class ApplicationPublisher
{
    public TResult ExecuteQuery<TResult>(IQuery<TResult> queryMessage, bool throwIfUnhandled = true) =>
        _publisher.ExecuteQuery(queryMessage, throwIfUnhandled);

    public ValueTask<TResult> ExecuteQueryAsync<TResult>(
        IQuery<TResult> queryMessage,
        CancellationToken cancellationToken = default) =>
        _publisher.ExecuteQueryAsync(queryMessage, cancellationToken);

    public ValueTask<TResult> ExecuteQueryAsync<TResult>(
        IQuery<TResult> queryMessage,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default) =>
        _publisher.ExecuteQueryAsync(queryMessage, throwIfUnhandled, cancellationToken);
}
