// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public class QueryPublisher : IQueryPublisher
    {
        private readonly IPublisher _publisher;

        public QueryPublisher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public IEnumerable<TResult> Execute<TResult>(IQuery<TResult> queryMessage) =>
            _publisher.Publish<TResult>(queryMessage);

        public Task<IEnumerable<TResult>> ExecuteAsync<TResult>(IQuery<TResult> queryMessage) =>
            _publisher.PublishAsync<TResult>(queryMessage);
    }
}