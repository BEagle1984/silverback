// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <inheritdoc />
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

        /// <inheritdoc />
        public void Execute(ICommand commandMessage)
        {
            _publisher.Publish(commandMessage);
        }

        /// <inheritdoc />
        public void Execute(IEnumerable<ICommand> commandMessages)
        {
            _publisher.Publish(commandMessages);
        }

        /// <inheritdoc />
        public TResult Execute<TResult>(ICommand<TResult> commandMessage)
        {
            return _publisher.Publish<TResult>(commandMessage).SingleOrDefault();
        }

        /// <inheritdoc />
        public IReadOnlyCollection<TResult> Execute<TResult>(IEnumerable<ICommand<TResult>> commandMessages)
        {
            return _publisher.Publish<TResult>(commandMessages);
        }

        /// <inheritdoc />
        public Task ExecuteAsync(ICommand commandMessage)
        {
            return _publisher.PublishAsync(commandMessage);
        }

        /// <inheritdoc />
        public Task ExecuteAsync(IEnumerable<ICommand> commandMessages)
        {
            return _publisher.PublishAsync(commandMessages);
        }

        /// <inheritdoc />
        public async Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> commandMessage)
        {
            return (await _publisher.PublishAsync<TResult>(commandMessage)).SingleOrDefault();
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<TResult>> ExecuteAsync<TResult>(IEnumerable<ICommand<TResult>> commandMessages)
        {
            return _publisher.PublishAsync<TResult>(commandMessages);
        }
    }
}
