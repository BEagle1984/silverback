// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Producing.Enrichers;

/// <summary>
///     A generic enricher that adds a message header according to a static name/value pair or a provider
///     function.
/// </summary>
public class StaticOutboundHeadersEnricher : GenericOutboundHeadersEnricher<object>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StaticOutboundHeadersEnricher" /> class.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="value">
    ///     The header value.
    /// </param>
    public StaticOutboundHeadersEnricher(string name, object? value)
        : base(name, value)
    {
    }
}
