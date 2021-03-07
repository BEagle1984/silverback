// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Diagnostics
{
    internal static class LogEventExtensions
    {
        public static LogEvent Enrich(this LogEvent logEvent, IBrokerLogEnricher logEnricher)
        {
            var message = $"{logEvent.Message} | " +
                          "endpointName: {endpointName}, " +
                          "messageType: {messageType}, " +
                          "messageId: {messageId}, " +
                          $"{logEnricher.AdditionalPropertyName1}: {{{logEnricher.AdditionalPropertyName1}}}, " +
                          $"{logEnricher.AdditionalPropertyName2}: {{{logEnricher.AdditionalPropertyName2}}}";

            return new LogEvent(
                logEvent.Level,
                logEvent.EventId,
                message);
        }
    }
}
