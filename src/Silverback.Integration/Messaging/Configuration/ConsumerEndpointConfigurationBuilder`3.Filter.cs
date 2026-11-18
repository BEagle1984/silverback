// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Consuming.Filter;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Implements the <c>Filter</c> methods.
/// </content>
public abstract partial class ConsumerEndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
{
    /// <summary>
    ///     Sets the filter used to filter out the consumed messages that should not be processed, replacing any previously configured
    ///     filter. The filtered messages aren't delivered to any subscriber and are committed (or acknowledged) like the messages that no
    ///     subscriber handles. The messages of other types than <typeparamref name="TMessage" /> are always processed.
    /// </summary>
    /// <param name="filter">
    ///     The predicate that returns <c>true</c> for the messages to be processed. It receives <c>null</c> for a tombstone.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Filter(Func<TMessage?, bool> filter)
    {
        Check.NotNull(filter);

        _filter = new GenericInboundMessageFilter<TMessage>(filter);

        return This;
    }

    /// <summary>
    ///     Sets the filter used to filter out the consumed messages that should not be processed, replacing any previously configured
    ///     filter. The filtered messages aren't delivered to any subscriber and are committed (or acknowledged) like the messages that no
    ///     subscriber handles. The messages of other types than <typeparamref name="TMessage" /> are always processed.
    /// </summary>
    /// <param name="filter">
    ///     The predicate that returns <c>true</c> for the envelopes to be processed.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Filter(Func<IInboundEnvelope<TMessage>, bool> filter)
    {
        Check.NotNull(filter);

        _filter = new GenericInboundMessageFilter<TMessage>(filter);

        return This;
    }

    /// <summary>
    ///     Sets the filter used to filter out the consumed messages that should not be processed, replacing any previously configured
    ///     filter. The filtered messages aren't delivered to any subscriber and are committed (or acknowledged) like the messages that no
    ///     subscriber handles.
    /// </summary>
    /// <param name="filter">
    ///     The <see cref="IInboundMessageFilter" /> to be used.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Filter(IInboundMessageFilter filter)
    {
        _filter = Check.NotNull(filter);

        return This;
    }

    /// <summary>
    ///     Sets a filter of the specified type, used to filter out the consumed messages that should not be processed, replacing any
    ///     previously configured filter. The filtered messages aren't delivered to any subscriber and are committed (or acknowledged) like
    ///     the messages that no subscriber handles. The filter is resolved via dependency injection for each message, from the service
    ///     scope of the message, therefore the type must be registered in the service collection.
    /// </summary>
    /// <typeparam name="TFilter">
    ///     The type of the <see cref="IInboundMessageFilter" /> to be used.
    /// </typeparam>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Filter<TFilter>()
        where TFilter : IInboundMessageFilter
    {
        _filter = new ServiceProviderInboundMessageFilter(typeof(TFilter));

        return This;
    }
}
