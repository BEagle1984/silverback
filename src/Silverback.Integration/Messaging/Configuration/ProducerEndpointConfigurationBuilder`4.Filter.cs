// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Filter;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Implements the <c>Filter</c> methods.
/// </content>
public abstract partial class ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
{
    /// <summary>
    ///     Sets the filter used to filter out the messages that should not be produced, replacing any previously configured filter.
    /// </summary>
    /// <param name="filter">
    ///     The predicate that returns <c>true</c> for the messages to be produced.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Filter(Func<TMessage?, bool> filter)
    {
        Check.NotNull(filter);

        _filter = new GenericOutboundMessageFilter<TMessage>(filter);

        return This;
    }

    /// <summary>
    ///     Sets the filter used to filter out the messages that should not be produced, replacing any previously configured filter.
    /// </summary>
    /// <param name="filter">
    ///     The predicate that returns <c>true</c> for the envelopes to be produced.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Filter(Func<IOutboundEnvelope<TMessage>, bool> filter)
    {
        Check.NotNull(filter);

        _filter = new GenericOutboundMessageFilter<TMessage>(filter);

        return This;
    }

    /// <summary>
    ///     Sets the filter used to filter out the messages that should not be produced, replacing any previously configured filter.
    /// </summary>
    /// <param name="filter">
    ///     The <see cref="IOutboundMessageFilter" /> to be used.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Filter(IOutboundMessageFilter filter)
    {
        _filter = Check.NotNull(filter);

        return This;
    }

    /// <summary>
    ///     Sets a filter of the specified type, used to filter out the messages that should not be produced, replacing any previously
    ///     configured filter. The filter is resolved via dependency injection for each message, therefore the type must be registered in
    ///     the service collection.
    /// </summary>
    /// <typeparam name="TFilter">
    ///     The type of the <see cref="IOutboundMessageFilter" /> to be used.
    /// </typeparam>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Filter<TFilter>()
        where TFilter : IOutboundMessageFilter
    {
        _filter = new ServiceProviderOutboundMessageFilter(typeof(TFilter));

        return This;
    }
}
