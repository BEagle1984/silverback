// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.Enrichers;

/// <summary>
///     A generic enricher that adds a message header according to a static name/value pair or a provider
///     function.
/// </summary>
public class GenericOutboundHeadersEnricher : GenericOutboundHeadersEnricher<object>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GenericOutboundHeadersEnricher" /> class.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="value">
    ///     The header value.
    /// </param>
    public GenericOutboundHeadersEnricher(string name, object? value)
        : base(name, value)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GenericOutboundHeadersEnricher" /> class.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="valueProvider">
    ///     The header value provider function.
    /// </param>
    public GenericOutboundHeadersEnricher(
        string name,
        Func<IOutboundEnvelope<object>, object?> valueProvider)
        : base(name, valueProvider)
    {
    }
}
