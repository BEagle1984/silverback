// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    public abstract class Producer : EndpointConnectedObject, IProducer
    {
        private static readonly DiagnosticListener _diagnosticListener = new DiagnosticListener(DiagnosticsConstants.DiagnosticListenerNameProducer);

        private readonly MessageKeyProvider _messageKeyProvider;
        private readonly MessageLogger _messageLogger;
        private readonly ILogger<Producer> _logger;

        protected Producer(IBroker broker, IEndpoint endpoint, MessageKeyProvider messageKeyProvider,
            ILogger<Producer> logger, MessageLogger messageLogger)
            : base(broker, endpoint)
        {
            _messageKeyProvider = messageKeyProvider;
            _logger = logger;
            _messageLogger = messageLogger;
        }

        public void Produce(object message, IEnumerable<MessageHeader> headers = null)
        {
            if(headers == null)
            {
                headers = Enumerable.Empty<MessageHeader>();
            }

            // TODO: 1. Not if diagnostics source isn't enabled.
            // 2. If current is empty, create a new activity.
            // 3. Ensure that there is no header with this name.
            // 4. Also use for ProduceAsync
            var enrichedHeaders = new List<MessageHeader>(headers);

            // TODO: Start just the activity, if there is no diagnosticListener.
            // But also give a way to disable the feature.
            if (_diagnosticListener.IsEnabled())
            {
                var activity = new Activity(DiagnosticsConstants.ActivityNameMessageProducing);
                _diagnosticListener.StartActivity(activity, new { message });

                enrichedHeaders.Add(new MessageHeader(DiagnosticsConstants.CorrelationIdHeaderKey, activity.Id));
                enrichedHeaders.Add(new MessageHeader(DiagnosticsConstants.CorrelationBaggageHeaderKey, BaggageConverter.Serialize(activity.Baggage)));
                try
                {
                    InternalProduce(message, enrichedHeaders);
                }
                finally
                {
                    _diagnosticListener.StopActivity(activity, new { message });
                }
            }
            else
            {
                InternalProduce(message, enrichedHeaders);
            }
        }

        private void InternalProduce(object message, List<MessageHeader> enrichedHeaders)
        {
            GetMessageContentChunks(message)
                .ForEach(x => Produce(x.message, x.serializedMessage, enrichedHeaders));
        }

        public Task ProduceAsync(object message, IEnumerable<MessageHeader> headers = null) =>
                GetMessageContentChunks(message)
                    .ForEachAsync(x => ProduceAsync(x.message, x.serializedMessage, headers));

        private IEnumerable<(object message, byte[] serializedMessage)> GetMessageContentChunks(object message)
        {
            _messageKeyProvider.EnsureKeyIsInitialized(message);
            Trace(message);

            return ChunkProducer.ChunkIfNeeded(
                _messageKeyProvider.GetKey(message, false),
                message,
                (Endpoint as IProducerEndpoint)?.Chunk,
                Endpoint.Serializer);
        }

        private void Trace(object message) =>
            _messageLogger.LogTrace(_logger, "Producing message.", message, Endpoint);

        protected abstract void Produce(object message, byte[] serializedMessage, IEnumerable<MessageHeader> headers);

        protected abstract Task ProduceAsync(object message, byte[] serializedMessage, IEnumerable<MessageHeader> headers);
    }

    public abstract class Producer<TBroker, TEndpoint> : Producer
        where TBroker : class, IBroker
        where TEndpoint : class, IEndpoint
    {
        protected Producer(IBroker broker, IEndpoint endpoint, MessageKeyProvider messageKeyProvider,
            ILogger<Producer> logger, MessageLogger messageLogger)
            : base(broker, endpoint, messageKeyProvider, logger, messageLogger)
        {
        }

        protected new TBroker Broker => (TBroker)base.Broker;

        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}