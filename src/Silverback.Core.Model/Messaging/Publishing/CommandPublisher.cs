// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public class CommandPublisher : ICommandPublisher
    {
        private readonly IPublisher _publisher;

        public CommandPublisher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public void Execute(ICommand commandMessage) => _publisher.Publish(commandMessage);

        public Task ExecuteAsync(ICommand commandMessage) => _publisher.PublishAsync(commandMessage);

        public void Execute(IEnumerable<ICommand> commandMessages) => _publisher.Publish(commandMessages);

        public Task ExecuteAsync(IEnumerable<ICommand> commandMessages) => _publisher.PublishAsync(commandMessages);

        public TResult Execute<TResult>(ICommand<TResult> commandMessage) =>
            _publisher.Publish<TResult>(commandMessage).SingleOrDefault();

        public async Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> commandMessage) =>
            (await _publisher.PublishAsync<TResult>(commandMessage)).SingleOrDefault();

        public IEnumerable<TResult> Execute<TResult>(IEnumerable<ICommand<TResult>> commandMessages) =>
            _publisher.Publish<TResult>(commandMessages);

        public Task<IEnumerable<TResult>> ExecuteAsync<TResult>(IEnumerable<ICommand<TResult>> commandMessages) =>
            _publisher.PublishAsync<TResult>(commandMessages);
    }
}