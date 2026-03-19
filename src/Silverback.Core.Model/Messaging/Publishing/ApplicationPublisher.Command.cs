// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing;

internal partial class ApplicationPublisher
{
    public void ExecuteCommand(ICommand commandMessage, bool throwIfUnhandled = true) =>
        _publisher.ExecuteCommand(commandMessage, throwIfUnhandled);

    public TResult ExecuteCommand<TResult>(ICommand<TResult> commandMessage, bool throwIfUnhandled = true) =>
        _publisher.ExecuteCommand(commandMessage, throwIfUnhandled);

    public Task ExecuteCommandAsync(ICommand commandMessage, CancellationToken cancellationToken = default) =>
        _publisher.ExecuteCommandAsync(commandMessage, cancellationToken);

    public Task ExecuteCommandAsync(
        ICommand commandMessage,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default) =>
        _publisher.ExecuteCommandAsync(commandMessage, throwIfUnhandled, cancellationToken);

    public Task<TResult> ExecuteCommandAsync<TResult>(
        ICommand<TResult> commandMessage,
        CancellationToken cancellationToken = default) =>
        _publisher.ExecuteCommandAsync(commandMessage, cancellationToken);

    public Task<TResult> ExecuteCommandAsync<TResult>(
        ICommand<TResult> commandMessage,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default) =>
        _publisher.ExecuteCommandAsync(commandMessage, throwIfUnhandled, cancellationToken);
}
