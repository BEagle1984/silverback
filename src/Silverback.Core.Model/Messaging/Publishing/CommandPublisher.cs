// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
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

        /// <inheritdoc cref="ICommandPublisher.Execute(IEnumerable{ICommand})" />
        public void Execute(IEnumerable<ICommand> commandMessages) => _publisher.Publish(commandMessages);

        /// <inheritdoc cref="ICommandPublisher.Execute(IEnumerable{ICommand}, bool)" />
        public void Execute(IEnumerable<ICommand> commandMessages, bool throwIfUnhandled) =>
            _publisher.Publish(commandMessages, throwIfUnhandled);

        /// <inheritdoc cref="ICommandPublisher.Execute{TResult}(ICommand{TResult})" />
        public TResult Execute<TResult>(ICommand<TResult> commandMessage) =>
            _publisher.Publish<TResult>(commandMessage).SingleOrDefault();

        /// <inheritdoc cref="ICommandPublisher.Execute{TResult}(ICommand{TResult}, bool)" />
        public TResult Execute<TResult>(ICommand<TResult> commandMessage, bool throwIfUnhandled) =>
            _publisher.Publish<TResult>(commandMessage, throwIfUnhandled).SingleOrDefault();

        /// <inheritdoc cref="ICommandPublisher.Execute{TResult}(IEnumerable{ICommand{TResult}})" />
        public IReadOnlyCollection<TResult> Execute<TResult>(IEnumerable<ICommand<TResult>> commandMessages) =>
            _publisher.Publish<TResult>(commandMessages);

        /// <inheritdoc cref="ICommandPublisher.Execute{TResult}(IEnumerable{ICommand{TResult}}, bool)" />
        public IReadOnlyCollection<TResult> Execute<TResult>(
            IEnumerable<ICommand<TResult>> commandMessages,
            bool throwIfUnhandled) => _publisher.Publish<TResult>(commandMessages, throwIfUnhandled);

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync(ICommand)" />
        public Task ExecuteAsync(ICommand commandMessage) => _publisher.PublishAsync(commandMessage);

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync(ICommand, bool)" />
        public Task ExecuteAsync(ICommand commandMessage, bool throwIfUnhandled) =>
            _publisher.PublishAsync(commandMessage, throwIfUnhandled);

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync(IEnumerable{ICommand})" />
        public Task ExecuteAsync(IEnumerable<ICommand> commandMessages) => _publisher.PublishAsync(commandMessages);

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync(IEnumerable{ICommand}, bool)" />
        public Task ExecuteAsync(IEnumerable<ICommand> commandMessages, bool throwIfUnhandled) =>
            _publisher.PublishAsync(commandMessages, throwIfUnhandled);

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync{TResult}(ICommand{TResult})" />
        public async Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> commandMessage) =>
            (await _publisher.PublishAsync<TResult>(commandMessage)
                .ConfigureAwait(false))
            .SingleOrDefault();

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync{TResult}(ICommand{TResult}, bool)" />
        public async Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> commandMessage, bool throwIfUnhandled) =>
            (await _publisher.PublishAsync<TResult>(commandMessage, throwIfUnhandled)
                .ConfigureAwait(false))
            .SingleOrDefault();

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync{TResult}(IEnumerable{ICommand{TResult}})" />
        public Task<IReadOnlyCollection<TResult>>
            ExecuteAsync<TResult>(IEnumerable<ICommand<TResult>> commandMessages) =>
            _publisher.PublishAsync<TResult>(commandMessages);

        /// <inheritdoc cref="ICommandPublisher.ExecuteAsync{TResult}(IEnumerable{ICommand{TResult}}, bool)" />
        public Task<IReadOnlyCollection<TResult>> ExecuteAsync<TResult>(
            IEnumerable<ICommand<TResult>> commandMessages,
            bool throwIfUnhandled) => _publisher.PublishAsync<TResult>(commandMessages, throwIfUnhandled);
    }
}
