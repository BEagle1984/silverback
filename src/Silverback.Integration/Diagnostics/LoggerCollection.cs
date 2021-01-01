// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Diagnostics
{
    // TODO: Test
    internal class LoggerCollection
    {
        private readonly LogAdditionalArguments<IRawInboundEnvelope> _defaultInboundArguments;

        private readonly LogAdditionalArguments<IOutboundEnvelope> _defaultOutboundArguments;

        private readonly InboundLogger _defaultInboundLogger;

        private readonly OutboundLogger _defaultOutboundLogger;

        private readonly Dictionary<Type, InboundLogger> _inboundLoggers = new();

        private readonly Dictionary<Type, OutboundLogger> _outboundLoggers = new();

        public LoggerCollection()
        {
            _defaultInboundArguments = new("unused1", _ => null, "unused2", _ => null);
            _defaultOutboundArguments = new("unused1", _ => null, "unused2", _ => null);

            _defaultInboundLogger = new(_defaultInboundArguments);
            _defaultOutboundLogger = new(_defaultOutboundArguments);
        }

        public void AddInbound<TEndpoint>(
            string name1,
            Func<IRawInboundEnvelope, string?> valueProvider1)
            where TEndpoint : IConsumerEndpoint
        {
            Check.NotEmpty(name1, nameof(name1));
            Check.NotNull(valueProvider1, nameof(valueProvider1));

            AddInbound<TEndpoint>(
                name1,
                valueProvider1,
                _defaultInboundArguments.Argument2.Name,
                endpoint => _defaultInboundArguments.Argument2.ValueProvider(endpoint));
        }

        public void AddInbound<TEndpoint>(
            string name1,
            Func<IRawInboundEnvelope, string?> valueProvider1,
            string name2,
            Func<IRawInboundEnvelope, string?> valueProvider2)
            where TEndpoint : IConsumerEndpoint
        {
            Check.NotEmpty(name1, nameof(name1));
            Check.NotNull(valueProvider1, nameof(valueProvider1));
            Check.NotEmpty(name2, nameof(name1));
            Check.NotNull(valueProvider2, nameof(valueProvider2));

            var additionalArguments = new LogAdditionalArguments<IRawInboundEnvelope>(
                name1,
                valueProvider1,
                name2,
                valueProvider2);

            _inboundLoggers.Add(
                typeof(TEndpoint),
                new InboundLogger(additionalArguments));
        }

        public InboundLogger GetInboundLogger(Type endpointType)
        {
            if (!_inboundLoggers.TryGetValue(endpointType, out var actions))
                actions = _defaultInboundLogger;

            return actions;
        }

        public void AddOutbound<TEndpoint>(
            string name1,
            Func<IOutboundEnvelope, string?> valueProvider1)
            where TEndpoint : IProducerEndpoint
        {
            Check.NotEmpty(name1, nameof(name1));
            Check.NotNull(valueProvider1, nameof(valueProvider1));

            AddOutbound<TEndpoint>(
                name1,
                valueProvider1,
                _defaultOutboundArguments.Argument2.Name,
                endpoint => _defaultOutboundArguments.Argument2.ValueProvider(endpoint));
        }

        public void AddOutbound<TEndpoint>(
            string name1,
            Func<IOutboundEnvelope, string?> valueProvider1,
            string name2,
            Func<IOutboundEnvelope, string?> valueProvider2)
            where TEndpoint : IProducerEndpoint
        {
            Check.NotEmpty(name1, nameof(name1));
            Check.NotNull(valueProvider1, nameof(valueProvider1));
            Check.NotEmpty(name2, nameof(name1));
            Check.NotNull(valueProvider2, nameof(valueProvider2));

            var additionalArguments = new LogAdditionalArguments<IOutboundEnvelope>(
                name1,
                valueProvider1,
                name2,
                valueProvider2);

            _outboundLoggers.Add(
                typeof(TEndpoint),
                new OutboundLogger(additionalArguments));
        }

        public OutboundLogger GetOutboundLogger(Type endpointType)
        {
            if (!_outboundLoggers.TryGetValue(endpointType, out var actions))
                actions = _defaultOutboundLogger;

            return actions;
        }
    }
}
