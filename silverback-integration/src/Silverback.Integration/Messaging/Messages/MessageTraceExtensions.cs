// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.Messages
{
    // TODO: Review and test
    public static class MessageTraceExtensions
    {
        public static void LogTrace(this ILogger logger, string logMessage, IMessage message, IEndpoint endpoint = null) => 
            Log(logger, LogLevel.Trace, null, logMessage, message, endpoint);

        public static void LogWarning(this ILogger logger, Exception exception, string logMessage, IMessage message, IEndpoint endpoint = null) =>
            Log(logger, LogLevel.Warning, exception, logMessage, message, endpoint);

        public static void LogCritical(this ILogger logger, Exception exception, string logMessage, IMessage message, IEndpoint endpoint = null) =>
            Log(logger, LogLevel.Critical, exception, logMessage, message, endpoint);

        public static void Log(this ILogger logger, LogLevel logLevel, Exception exception, string logMessage, IMessage message, IEndpoint endpoint = null)
        {
            var integrationMassage =  message as IIntegrationMessage;
            logger.Log(logLevel, exception, logMessage + " {{id=\"{messageId}\", endpoint=\"{endpointName}\", type=\"{messageType}\"}}", integrationMassage?.Id, endpoint?.Name, message?.GetType().Name);
        }
    }
}
