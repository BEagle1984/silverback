// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
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

        public IEnumerable<TResult> Execute<TResult>(ICommand<TResult> commandMessage) => 
            _publisher.Publish<TResult>(commandMessage);

        public Task<IEnumerable<TResult>> ExecuteAsync<TResult>(ICommand<TResult> commandMessage) => 
            _publisher.PublishAsync<TResult>(commandMessage);
    }
}