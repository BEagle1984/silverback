// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.Filter;

/// <summary>
///     Can be used to filter the consumed messages based on the message content.
/// </summary>
/// <remarks>
///     The filter applies only to the messages of type <typeparamref name="TMessage" />: the messages of the other types are always
///     processed.
/// </remarks>
/// <typeparam name="TMessage">
///     The type of the messages to be filtered.
/// </typeparam>
public class GenericInboundMessageFilter<TMessage> : IInboundMessageFilter
{
    private readonly Func<IInboundEnvelope<TMessage>, bool> _filter;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GenericInboundMessageFilter{TMessage}" /> class.
    /// </summary>
    /// <param name="filter">
    ///     The filter function, which receives <c>null</c> for a tombstone. If it returns <c>true</c> the message will be processed,
    ///     otherwise it will be skipped.
    /// </param>
    public GenericInboundMessageFilter(Func<TMessage?, bool> filter)
    {
        Check.NotNull(filter, nameof(filter));

        _filter = envelope => filter.Invoke(envelope.Message);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GenericInboundMessageFilter{TMessage}" /> class.
    /// </summary>
    /// <param name="filter">
    ///     The filter function. If it returns <c>true</c> the message will be processed, otherwise it will be skipped.
    /// </param>
    public GenericInboundMessageFilter(Func<IInboundEnvelope<TMessage>, bool> filter)
    {
        _filter = Check.NotNull(filter, nameof(filter));
    }

    /// <inheritdoc cref="IInboundMessageFilter.ShouldProcessAsync" />
    public ValueTask<bool> ShouldProcessAsync(IInboundEnvelope envelope, CancellationToken cancellationToken)
    {
        Check.NotNull(envelope, nameof(envelope));

        // The filter isn't meant for the messages of the other types, which are therefore processed instead of being silently dropped
        if (envelope is not IInboundEnvelope<TMessage> typedEnvelope)
            return ValueTask.FromResult(true);

        return ValueTask.FromResult(_filter.Invoke(typedEnvelope));
    }
}
