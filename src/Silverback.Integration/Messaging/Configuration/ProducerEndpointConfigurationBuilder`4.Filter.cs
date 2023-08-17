// Copyright (c) 2023 Sergio Aquilini
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
    ///     Adds the specified filter, to filter out the messages that should not be produced.
    /// </summary>
    /// <param name="filter">
    ///     The filter to be added.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Filter(Func<TMessage?, bool> filter)
    {
        Check.NotNull(filter, nameof(filter));

        _filter = new GenericOutboundMessageFilter<TMessage>(filter);

        return This;
    }

    /// <summary>
    ///     Adds the specified filter, to filter out the messages that should not be produced.
    /// </summary>
    /// <param name="filter">
    ///     The filter to be added.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Filter(Func<IOutboundEnvelope<TMessage>, bool> filter)
    {
        Check.NotNull(filter, nameof(filter));

        _filter = new GenericOutboundMessageFilter<TMessage>(filter);

        return This;
    }
}
