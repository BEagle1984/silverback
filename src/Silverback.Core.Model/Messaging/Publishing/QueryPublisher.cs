// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing;

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

    /// <inheritdoc cref="IPublisherBase.Context" />
    public SilverbackContext Context => _publisher.Context;

    /// <inheritdoc cref="IQueryPublisher.Execute{TResult}(IQuery{TResult})" />
    public TResult Execute<TResult>(IQuery<TResult> queryMessage) =>
        _publisher.Publish<TResult>(queryMessage, true).Single();

    /// <inheritdoc cref="IQueryPublisher.ExecuteAsync{TResult}(IQuery{TResult})" />
    public async ValueTask<TResult> ExecuteAsync<TResult>(IQuery<TResult> queryMessage) =>
        (await _publisher.PublishAsync<TResult>(queryMessage, true).ConfigureAwait(false)).Single();
}
