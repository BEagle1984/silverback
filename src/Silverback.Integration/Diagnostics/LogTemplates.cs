// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging;

namespace Silverback.Diagnostics
{
    internal class LogTemplates : ILogTemplates
    {
        private const string InboundArgumentsTemplate = " | " +
                                                        "consumerId: {consumerId}, " +
                                                        "endpointName: {endpointName}, " +
                                                        "failedAttempts: {failedAttempts}, " +
                                                        "messageType: {messageType}, " +
                                                        "messageId: {messageId}";

        private const string InboundSequenceArgumentsTemplate = " | " +
                                                                "consumerId: {consumerId}, " +
                                                                "endpointName: {endpointName}, " +
                                                                "failedAttempts: {failedAttempts}, " +
                                                                "messageType: {messageType}, " +
                                                                "messageId: {messageId}, " +
                                                                "sequenceType: {sequenceType}, " +
                                                                "sequenceId: {sequenceId}, " +
                                                                "sequenceLength: {sequenceLength}, " +
                                                                "sequenceIsNew: {sequenceIsNew}, " +
                                                                "sequenceIsComplete: {sequenceIsComplete}";

        private const string OutboundArgumentsTemplate = " | " +
                                                         "endpointName: {endpointName}, " +
                                                         "messageType: {messageType}, " +
                                                         "messageId: {messageId}";

        private readonly Dictionary<Type, string> _inboundLogMessageByEndpointType = new();

        private readonly Dictionary<Type, string> _inboundSequenceLogMessageByEndpointType = new();

        private readonly Dictionary<Type, string> _outboundLogMessageByEndpointType = new();

        private readonly Dictionary<Type, string[]> _inboundArgumentsByEndpointType = new();

        private readonly Dictionary<Type, string[]> _outboundArgumentsByEndpointType = new();

        public ILogTemplates ConfigureAdditionalData<TEndpoint>(params string[] additionalData)
        {
            var appendString = ", " + string.Join(", ", additionalData.Select(key => $"{key}: {{{key}}}"));

            _inboundArgumentsByEndpointType[typeof(TEndpoint)] = additionalData;
            _outboundArgumentsByEndpointType[typeof(TEndpoint)] = additionalData;

            _inboundLogMessageByEndpointType[typeof(TEndpoint)] =
                InboundArgumentsTemplate + appendString;
            _inboundSequenceLogMessageByEndpointType[typeof(TEndpoint)] =
                InboundSequenceArgumentsTemplate + appendString;
            _outboundLogMessageByEndpointType[typeof(TEndpoint)] =
                OutboundArgumentsTemplate + appendString;

            return this;
        }

        public string GetInboundMessageLogTemplate(IEndpoint? endpoint) =>
            GetMessageLogTemplate(endpoint, _inboundLogMessageByEndpointType, InboundArgumentsTemplate);

        public string GetInboundSequenceLogTemplate(IEndpoint? endpoint) =>
            GetMessageLogTemplate(endpoint, _inboundSequenceLogMessageByEndpointType, InboundSequenceArgumentsTemplate);

        public string[] GetInboundMessageArguments(IEndpoint? endpoint) =>
            GetMessageArguments(endpoint, _inboundArgumentsByEndpointType);

        public string GetOutboundMessageLogTemplate(IEndpoint? endpoint) =>
            GetMessageLogTemplate(endpoint, _outboundLogMessageByEndpointType, OutboundArgumentsTemplate);

        public string[] GetOutboundMessageArguments(IEndpoint? endpoint) =>
            GetMessageArguments(endpoint, _outboundArgumentsByEndpointType);

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
