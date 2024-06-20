// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <inheritdoc cref="ICommandPublisher" />
    public class CommandPublisher : ICommandPublisher
    {
        private readonly IPublisher _publisher;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandPublisher" /> class.
        /// </summary>
        /// <param name="publisher">
        ///     The <see cref="IPublisher" /> to be wrapped.
        /// </param>
        public CommandPublisher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        /// <inheritdoc cref="ICommandPublisher.Execute(ICommand)" />
        public void Execute(ICommand commandMessage) => _publisher.Publish(commandMessage);

        /// <inheritdoc cref="ICommandPublisher.Execute(ICommand, bool)" />
        public void Execute(ICommand commandMessage, bool throwIfUnhandled) =>
            _publisher.Publish(commandMessage, throwIfUnhandled);

        /// <inheritdoc cref="ICommandPublisher.Execute{TResult}(ICommand{TResult})" />
        public TResult Execute<TResult>(ICommand<TResult> commandMessage) =>
            _publisher.Publish<TResult>(commandMessage).SingleOrDefault();

        /// <inheritdoc cref="ICommandPublisher.Execute{TResult}(ICommand{TResult}, bool)" />
        public TResult Execute<TResult>(ICommand<TResult> commandMessage, bool throwIfUnhandled) =>
            _publisher.Publish<TResult>(commandMessage, throwIfUnhandled).SingleOrDefault();

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync(ICommand, CancellationToken)" />
        public Task ExecuteAsync(ICommand commandMessage, CancellationToken cancellationToken = default) =>
            _publisher.PublishAsync(commandMessage, cancellationToken);

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync(ICommand, bool, CancellationToken)" />
        public Task ExecuteAsync(
            ICommand commandMessage,
            bool throwIfUnhandled,
            CancellationToken cancellationToken = default) =>
            _publisher.PublishAsync(commandMessage, throwIfUnhandled, cancellationToken);

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync{TResult}(ICommand{TResult}, CancellationToken)" />
        public async Task<TResult> ExecuteAsync<TResult>(
            ICommand<TResult> commandMessage,
            CancellationToken cancellationToken = default) =>
            (await _publisher.PublishAsync<TResult>(commandMessage, cancellationToken)
                .ConfigureAwait(false))
            .SingleOrDefault();

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync{TResult}(ICommand{TResult}, bool, CancellationToken)" />
        public async Task<TResult> ExecuteAsync<TResult>(
            ICommand<TResult> commandMessage,
            bool throwIfUnhandled,
            CancellationToken cancellationToken = default) =>
            (await _publisher.PublishAsync<TResult>(commandMessage, throwIfUnhandled, cancellationToken)
                .ConfigureAwait(false))
            .SingleOrDefault();
    }
}
