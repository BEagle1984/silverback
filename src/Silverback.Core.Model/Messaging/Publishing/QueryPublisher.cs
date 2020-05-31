// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <inheritdoc cref="IQueryPublisher" />
    public class QueryPublisher : IQueryPublisher
    {
        private readonly IPublisher _publisher;

        /// <summary>
        ///     Initializes a new instance of the <see cref="QueryPublisher" /> class.
        /// </summary>
        /// <param name="publisher">
        ///     The <see cref="IPublisher" /> to be wrapped.
        /// </param>
        public QueryPublisher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        /// <inheritdoc cref="IQueryPublisher.Execute{TResult}(IQuery{TResult})" />
        public TResult Execute<TResult>(IQuery<TResult> queryMessage) =>
            _publisher.Publish<TResult>(queryMessage).SingleOrDefault();

        /// <inheritdoc cref="IQueryPublisher.Execute{TResult}(IQuery{TResult}, bool)" />
        public TResult Execute<TResult>(IQuery<TResult> queryMessage, bool throwIfUnhandled) =>
            _publisher.Publish<TResult>(queryMessage, throwIfUnhandled).SingleOrDefault();

        /// <inheritdoc cref="IQueryPublisher.Execute{TResult}(IEnumerable{IQuery{TResult}})" />
        public IReadOnlyCollection<TResult> Execute<TResult>(IEnumerable<IQuery<TResult>> queryMessages) =>
            _publisher.Publish<TResult>(queryMessages);

        /// <inheritdoc cref="IQueryPublisher.Execute{TResult}(IEnumerable{IQuery{TResult}}, bool)" />
        public IReadOnlyCollection<TResult> Execute<TResult>(
            IEnumerable<IQuery<TResult>> queryMessages,
            bool throwIfUnhandled) =>
            _publisher.Publish<TResult>(queryMessages, throwIfUnhandled);

        /// <inheritdoc cref="IQueryPublisher.ExecuteAsync{TResult}(IQuery{TResult})" />
        public async Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> queryMessage) =>
            (await _publisher.PublishAsync<TResult>(queryMessage)).SingleOrDefault();

        /// <inheritdoc cref="IQueryPublisher.ExecuteAsync{TResult}(IQuery{TResult}, bool)" />
        public async Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> queryMessage, bool throwIfUnhandled) =>
            (await _publisher.PublishAsync<TResult>(queryMessage, throwIfUnhandled)).SingleOrDefault();

        /// <inheritdoc cref="IQueryPublisher.ExecuteAsync{TResult}(IEnumerable{IQuery{TResult}})" />
        public Task<IReadOnlyCollection<TResult>> ExecuteAsync<TResult>(IEnumerable<IQuery<TResult>> queryMessages) =>
            _publisher.PublishAsync<TResult>(queryMessages);

        /// <inheritdoc cref="IQueryPublisher.ExecuteAsync{TResult}(IEnumerable{IQuery{TResult}}, bool)" />
        public Task<IReadOnlyCollection<TResult>> ExecuteAsync<TResult>(
            IEnumerable<IQuery<TResult>> queryMessages,
            bool throwIfUnhandled) =>
            _publisher.PublishAsync<TResult>(queryMessages, throwIfUnhandled);
    }
}
