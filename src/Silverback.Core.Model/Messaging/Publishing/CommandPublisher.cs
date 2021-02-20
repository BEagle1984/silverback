// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
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

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync(ICommand)" />
        public Task ExecuteAsync(ICommand commandMessage) => _publisher.PublishAsync(commandMessage);

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync(ICommand, bool)" />
        public Task ExecuteAsync(ICommand commandMessage, bool throwIfUnhandled) =>
            _publisher.PublishAsync(commandMessage, throwIfUnhandled);

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync{TResult}(ICommand{TResult})" />
        public async Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> commandMessage) =>
            (await _publisher.PublishAsync<TResult>(commandMessage)
                .ConfigureAwait(false))
            .SingleOrDefault();

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync{TResult}(ICommand{TResult}, bool)" />
        public async Task<TResult> ExecuteAsync<TResult>(
            ICommand<TResult> commandMessage,
            bool throwIfUnhandled) =>
            (await _publisher.PublishAsync<TResult>(commandMessage, throwIfUnhandled)
                .ConfigureAwait(false))
            .SingleOrDefault();
    }
}
