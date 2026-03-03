// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing;

internal partial class ApplicationPublisher
{
    public TResult ExecuteQuery<TResult>(IQuery<TResult> queryMessage, bool throwIfUnhandled = true) =>
        Publish<TResult>(queryMessage, throwIfUnhandled).Single();

    public async ValueTask<TResult> ExecuteQueryAsync<TResult>(
        IQuery<TResult> queryMessage,
        CancellationToken cancellationToken = default) =>
        (await PublishAsync<TResult>(queryMessage, true, cancellationToken).ConfigureAwait(false)).Single();

    public async ValueTask<TResult> ExecuteQueryAsync<TResult>(
        IQuery<TResult> queryMessage,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default) =>
        (await PublishAsync<TResult>(queryMessage, throwIfUnhandled, cancellationToken).ConfigureAwait(false)).Single();
}
