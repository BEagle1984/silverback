// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing.Filter;

/// <summary>
///     Can be used to filter the messages to be produced based on the message content.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be filtered.
/// </typeparam>
public class GenericOutboundMessageFilter<TMessage> : IOutboundMessageFilter
{
    private readonly Func<IOutboundEnvelope<TMessage>, bool> _filter;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GenericOutboundMessageFilter{TMessage}" /> class.
    /// </summary>
    /// <param name="filter">
    ///     The filter function. If it returns <c>true</c> the message will be produced, otherwise it will be skipped.
    /// </param>
    public GenericOutboundMessageFilter(Func<TMessage?, bool> filter)
    {
        Check.NotNull(filter, nameof(filter));

        _filter = envelope => filter.Invoke(envelope.Message);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GenericOutboundMessageFilter{TMessage}" /> class.
    /// </summary>
    /// <param name="filter">
    ///     The filter function. If it returns <c>true</c> the message will be produced, otherwise it will be skipped.
    /// </param>
    public GenericOutboundMessageFilter(Func<IOutboundEnvelope<TMessage>, bool> filter)
    {
        _filter = Check.NotNull(filter, nameof(filter));
    }

    /// <inheritdoc cref="IOutboundMessageFilter.ShouldProduceAsync" />
    public ValueTask<bool> ShouldProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken)
    {
        Check.NotNull(envelope, nameof(envelope));

        if (envelope is not IOutboundEnvelope<TMessage> typedEnvelope)
            return ValueTask.FromResult(false);

        return ValueTask.FromResult(_filter.Invoke(typedEnvelope));
    }
}
