// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing;

internal partial class ApplicationPublisher
{
    public void ExecuteCommand(ICommand commandMessage, bool throwIfUnhandled = true) =>
        Publish(commandMessage, throwIfUnhandled);

    public TResult ExecuteCommand<TResult>(ICommand<TResult> commandMessage, bool throwIfUnhandled = true) =>
        Publish<TResult>(commandMessage, throwIfUnhandled).Single();

    public Task ExecuteCommandAsync(ICommand commandMessage, CancellationToken cancellationToken = default) =>
        PublishAsync(commandMessage, true, cancellationToken);

    public Task ExecuteCommandAsync(
        ICommand commandMessage,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default) =>
        PublishAsync(commandMessage, throwIfUnhandled, cancellationToken);

    public async Task<TResult> ExecuteCommandAsync<TResult>(
        ICommand<TResult> commandMessage,
        CancellationToken cancellationToken = default) =>
        (await PublishAsync<TResult>(commandMessage, true, cancellationToken).ConfigureAwait(false)).Single();

    public async Task<TResult> ExecuteCommandAsync<TResult>(
        ICommand<TResult> commandMessage,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default) =>
        (await PublishAsync<TResult>(commandMessage, throwIfUnhandled, cancellationToken).ConfigureAwait(false)).Single();
}
