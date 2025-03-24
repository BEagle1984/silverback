// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Wraps the <see cref="Confluent.Kafka.AdminClientBuilder" />.
/// </summary>
public class ConfluentAdminClientFactory : IConfluentAdminClientFactory
{
    private readonly ISilverbackLogger _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConfluentAdminClientFactory" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    public ConfluentAdminClientFactory(ISilverbackLogger<ConfluentAdminClientFactory> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc cref="IConfluentAdminClientFactory.GetClient" />
    public IAdminClient GetClient(ClientConfig config) =>
        new AdminClientBuilder(config)
            .SetErrorHandler((_, error) => OnError(error, _logger))
            .SetLogHandler((_, logMessage) => OnLog(logMessage, _logger))
            .Build();

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private static void OnError(Error error, ISilverbackLogger logger)
    {
        if (error.IsFatal)
            logger.LogConfluentAdminClientFatalError(error);
        else
            logger.LogConfluentAdminClientError(error);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private static void OnLog(
        LogMessage logMessage,
        ISilverbackLogger logger)
    {
        switch (logMessage.Level)
        {
            case SyslogLevel.Emergency:
            case SyslogLevel.Alert:
            case SyslogLevel.Critical:
                logger.LogConfluentAdminClientLogCritical(logMessage);
                break;
            case SyslogLevel.Error:
                logger.LogConfluentAdminClientLogError(logMessage);
                break;
            case SyslogLevel.Warning:
                logger.LogConfluentAdminClientLogWarning(logMessage);
                break;
            case SyslogLevel.Notice:
            case SyslogLevel.Info:
                logger.LogConfluentAdminClientLogInformation(logMessage);
                break;
            default:
                logger.LogConfluentAdminClientLogDebug(logMessage);
                break;
        }
    }
}
