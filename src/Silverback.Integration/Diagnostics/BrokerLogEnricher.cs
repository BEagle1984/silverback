// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Diagnostics;

/// <inheritdoc cref="IBrokerLogEnricher" />
public abstract class BrokerLogEnricher : IBrokerLogEnricher
{
    /// <summary>
    ///     Gets the name of the first additional property.
    /// </summary>
    protected abstract string AdditionalPropertyName1 { get; }

    /// <summary>
    ///     Gets the name of the second additional property.
    /// </summary>
    protected abstract string AdditionalPropertyName2 { get; }

    /// <inheritdoc cref="IBrokerLogEnricher.Define" />
    public Action<ILogger, string, string?, string?, string?, string?, Exception?> Define(LogEvent logEvent) =>
        SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(Enrich(logEvent));

    /// <inheritdoc cref="IBrokerLogEnricher.Define{T}" />
    public Action<ILogger, T, string, string?, string?, string?, string?, Exception?> Define<T>(LogEvent logEvent) =>
        SilverbackLoggerMessage.Define<T, string, string?, string?, string?, string?>(Enrich(logEvent));

    /// <inheritdoc cref="IBrokerLogEnricher.Enrich(LogEvent)" />
    public LogEvent Enrich(LogEvent logEvent)
    {
        Check.NotNull(logEvent, nameof(logEvent));

        return new LogEvent(logEvent.Level, logEvent.EventId, Enrich(logEvent.Message));
    }

    /// <inheritdoc cref="IBrokerLogEnricher.Enrich(string)" />
    public string Enrich(string message) =>
        $"{message} | " +
        "endpointName: {endpointName}, " +
        "messageType: {messageType}, " +
        "messageId: {messageId}, " +
        $"{AdditionalPropertyName1}: {{{AdditionalPropertyName1}}}, " +
        $"{AdditionalPropertyName2}: {{{AdditionalPropertyName2}}}";

    /// <inheritdoc cref="IBrokerLogEnricher.GetAdditionalValues" />
    public abstract (string? Value1, string? Value2) GetAdditionalValues(
        IReadOnlyCollection<MessageHeader>? headers,
        IBrokerMessageIdentifier? brokerMessageIdentifier);
}
