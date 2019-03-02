// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
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

        public TResult Execute<TResult>(IQuery<TResult> queryMessage) =>
            _publisher.Publish<TResult>(queryMessage).SingleOrDefault();

        public async Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> queryMessage) =>
            (await _publisher.PublishAsync<TResult>(queryMessage)).SingleOrDefault();

        public IEnumerable<TResult> Execute<TResult>(IEnumerable<IQuery<TResult>> queryMessages) =>
            _publisher.Publish<TResult>(queryMessages);

        public Task<IEnumerable<TResult>> ExecuteAsync<TResult>(IEnumerable<IQuery<TResult>> queryMessages) =>
            _publisher.PublishAsync<TResult>(queryMessages);
    }
}