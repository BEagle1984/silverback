// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Handles the templates of the string appended to the log message by the
    ///     <see cref="SilverbackLoggerExtensions" /> to log additional message data such as endpoint name,
    ///     message type, etc.
    /// </summary>
    public static class LogTemplates
    {
        private const string InboundArgumentsTemplate = " | " +
                                                        "endpointName: {endpointName}, " +
                                                        "failedAttempts: {failedAttempts}, " +
                                                        "messageType: {messageType}, " +
                                                        "messageId: {messageId}";

        private const string InboundBatchArgumentsTemplate = " | " +
                                                             "endpointName: {endpointName}, " +
                                                             "failedAttempts: {failedAttempts}, " +
                                                             "batchId: {batchId}, " +
                                                             "batchSize: {batchSize}";

        private const string OutboundArgumentsTemplate = " | " +
                                                         "endpointName: {endpointName}, " +
                                                         "messageType: {messageType}, " +
                                                         "messageId: {messageId}";

        private const string OutboundBatchArgumentsTemplate = " | " +
                                                              "endpointName: {endpointName}";

        private static readonly Dictionary<Type, string> InboundLogMessageByEndpointType =
            new Dictionary<Type, string>();

        private static readonly Dictionary<Type, string> OutboundLogMessageByEndpointType =
            new Dictionary<Type, string>();

        private static readonly Dictionary<Type, string[]> InboundArgumentsByEndpointType =
            new Dictionary<Type, string[]>();

        private static readonly Dictionary<Type, string[]> OutboundArgumentsByEndpointType =
            new Dictionary<Type, string[]>();

        /// <summary>
        ///     Configures the additional log data expected for the specified endpoint type.
        /// </summary>
        /// <typeparam name="TEndpoint">The type of the endpoint.</typeparam>
        /// <param name="additionalData">The keys of the additional log data.</param>
        public static void ConfigureAdditionalData<TEndpoint>(params string[] additionalData)
        {
            var appendString = ", " + string.Join(", ", additionalData.Select(key => $"{key}: {{{key}}}"));

            InboundArgumentsByEndpointType[typeof(TEndpoint)] = additionalData;
            OutboundArgumentsByEndpointType[typeof(TEndpoint)] = additionalData;

            InboundLogMessageByEndpointType[typeof(TEndpoint)] = InboundArgumentsTemplate + appendString;
            OutboundLogMessageByEndpointType[typeof(TEndpoint)] = OutboundArgumentsTemplate + appendString;
        }

        /// <summary>
        ///     Gets the message template to be appended to the logs related to a message consumed from the specified
        ///     endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>The message template.</returns>
        public static string GetInboundMessageLogTemplate(IEndpoint? endpoint) =>
            GetMessageLogTemplate(endpoint, InboundLogMessageByEndpointType, InboundArgumentsTemplate);

        /// <summary>
        ///     Gets the additional arguments expected for a message consumed from the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>The expected arguments.</returns>
        public static string[] GetInboundMessageArguments(IEndpoint? endpoint) =>
            GetMessageArguments(endpoint, InboundArgumentsByEndpointType);

        /// <summary>
        ///     Gets the message template to be appended to the logs related to a batch of messages consumed from the
        ///     specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>The message template.</returns>
        [SuppressMessage("", "CA1801", Justification = "Parameter here for consistency.")]
        public static string GetInboundBatchLogTemplate(IEndpoint? endpoint) => InboundBatchArgumentsTemplate;

        /// <summary>
        ///     Gets the message template to be appended to the logs related to a message produced to the specified
        ///     endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>The message template.</returns>
        public static string GetOutboundMessageLogTemplate(IEndpoint? endpoint) =>
            GetMessageLogTemplate(endpoint, OutboundLogMessageByEndpointType, OutboundArgumentsTemplate);

        /// <summary>
        ///     Gets the additional arguments expected for a message produced to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>The expected arguments.</returns>
        public static string[] GetOutboundMessageArguments(IEndpoint? endpoint) =>
            GetMessageArguments(endpoint, OutboundArgumentsByEndpointType);

        /// <summary>
        ///     Gets the message template to be appended to the logs related to a batch of messages produced to the
        ///     specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>The message template.</returns>
        [SuppressMessage("", "CA1801", Justification = "Parameter here for consistency.")]
        public static string GetOutboundBatchLogTemplate(IEndpoint? endpoint) => OutboundBatchArgumentsTemplate;

        private static string GetMessageLogTemplate(
            IEndpoint? endpoint,
            IDictionary<Type, string> dictionary,
            string defaultMessage)
        {
            if (endpoint == null)
                return defaultMessage;

            return dictionary.TryGetValue(endpoint.GetType(), out string message) ? message : defaultMessage;
        }

        private static string[] GetMessageArguments(IEndpoint? endpoint, IDictionary<Type, string[]> dictionary)
        {
            if (endpoint == null)
                return Array.Empty<string>();

            return dictionary.TryGetValue(endpoint.GetType(), out string[] args) ? args : Array.Empty<string>();
        }
    }
}
