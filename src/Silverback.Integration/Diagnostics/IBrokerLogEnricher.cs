// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Diagnostics;

/// <summary>
///     Provides enrichment for the logs written in the context of the consumers and producers.
/// </summary>
public interface IBrokerLogEnricher
{
    /// <summary>
    ///     Creates a delegate that can be invoked to log a message.
    /// </summary>
    /// <param name="logEvent">
    ///     The <see cref="LogEvent" /> to be logged.
    /// </param>
    /// <returns>
    ///     The delegate that can be used to log the message.
    /// </returns>
    Action<ILogger, string, string?, string?, string?, string?, Exception?> Define(LogEvent logEvent);

    /// <summary>
    ///     Creates a delegate that can be invoked to log a message.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the additional parameter (added before the ones from the enrichment).
    /// </typeparam>
    /// <param name="logEvent">
    ///     The <see cref="LogEvent" /> to be logged.
    /// </param>
    /// <returns>
    ///     The delegate that can be used to log the message.
    /// </returns>
    Action<ILogger, T, string, string?, string?, string?, string?, Exception?> Define<T>(LogEvent logEvent) =>
        SilverbackLoggerMessage.Define<T, string, string?, string?, string?, string?>(Enrich(logEvent));

    /// <summary>
    ///     Returns the values for the two additional properties.
    /// </summary>
    /// <param name="endpoint">
    ///     The target endpoint.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="brokerMessageIdentifier">
    ///     The message identifier at broker level (e.g. the Kafka offset).
    /// </param>
    /// <returns>
    ///     Returns a tuple containing the values for the two additional properties.
    /// </returns>
    (string? Value1, string? Value2) GetAdditionalValues(
        Endpoint endpoint,
        IReadOnlyCollection<MessageHeader>? headers,
        IBrokerMessageIdentifier? brokerMessageIdentifier);

    /// <summary>
    ///     Returns a new <see cref="LogEvent" /> corresponding to the specified <see cref="LogEvent" /> enriched with the message properties.
    /// </summary>
    /// <param name="logEvent">
    ///     The <see cref="LogEvent" /> to be enriched.
    /// </param>
    /// <returns>
    ///     The enriched <see cref="LogEvent" />.
    /// </returns>
    LogEvent Enrich(LogEvent logEvent);

    /// <summary>
    ///     Enriches the specified message adding the message properties.
    /// </summary>
    /// <param name="message">
    ///     The message to be enriched.
    /// </param>
    /// <returns>
    ///     The enriched message.
    /// </returns>
    string Enrich(string message);
}
