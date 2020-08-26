// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Messaging;

namespace Silverback.Diagnostics
{
    internal class LogTemplates : ILogTemplates
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

        private readonly Dictionary<Type, string> _inboundLogMessageByEndpointType = new Dictionary<Type, string>();

        private readonly Dictionary<Type, string> _outboundLogMessageByEndpointType = new Dictionary<Type, string>();

        private readonly Dictionary<Type, string[]> _inboundArgumentsByEndpointType = new Dictionary<Type, string[]>();

        private readonly Dictionary<Type, string[]> _outboundArgumentsByEndpointType = new Dictionary<Type, string[]>();

        public ILogTemplates ConfigureAdditionalData<TEndpoint>(params string[] additionalData)
        {
            var appendString = ", " + string.Join(", ", additionalData.Select(key => $"{key}: {{{key}}}"));

            _inboundArgumentsByEndpointType[typeof(TEndpoint)] = additionalData;
            _outboundArgumentsByEndpointType[typeof(TEndpoint)] = additionalData;

            _inboundLogMessageByEndpointType[typeof(TEndpoint)] = InboundArgumentsTemplate + appendString;
            _outboundLogMessageByEndpointType[typeof(TEndpoint)] = OutboundArgumentsTemplate + appendString;

            return this;
        }

        public string GetInboundMessageLogTemplate(IEndpoint? endpoint) =>
            GetMessageLogTemplate(endpoint, _inboundLogMessageByEndpointType, InboundArgumentsTemplate);

        public string[] GetInboundMessageArguments(IEndpoint? endpoint) =>
            GetMessageArguments(endpoint, _inboundArgumentsByEndpointType);

        [SuppressMessage("", "CA1801", Justification = "Parameter here for consistency.")]
        public string GetInboundBatchLogTemplate(IEndpoint? endpoint) => InboundBatchArgumentsTemplate;

        public string GetOutboundMessageLogTemplate(IEndpoint? endpoint) =>
            GetMessageLogTemplate(endpoint, _outboundLogMessageByEndpointType, OutboundArgumentsTemplate);

        public string[] GetOutboundMessageArguments(IEndpoint? endpoint) =>
            GetMessageArguments(endpoint, _outboundArgumentsByEndpointType);

        [SuppressMessage("", "CA1801", Justification = "Parameter here for consistency.")]
        public string GetOutboundBatchLogTemplate(IEndpoint? endpoint) => OutboundBatchArgumentsTemplate;

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
