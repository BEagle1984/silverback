// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    public abstract class Producer : EndpointConnectedObject, IProducer
    {
        private const string ActivityName = "kafkaMsg";
        private const string ActivityName = "kafkaMsg";
        private const string CorrelationIdHeaderKey = "CorrelationId"; // TODO: Config and share with consumer
        private const string CorrelationBaggageHeaderKey = "Correlation-Context"; // TODO: Config and share with consumer

        private readonly MessageKeyProvider _messageKeyProvider;
        private readonly MessageLogger _messageLogger;
        private readonly DiagnosticListener _diagnosticListener;
        private readonly ILogger<Producer> _logger;

        protected Producer(IBroker broker, IEndpoint endpoint, MessageKeyProvider messageKeyProvider,
            ILogger<Producer> logger, MessageLogger messageLogger, DiagnosticListener diagnosticListener)
            : base(broker, endpoint)
        {
            _messageKeyProvider = messageKeyProvider;
            _logger = logger;
            _messageLogger = messageLogger;
            _diagnosticListener = diagnosticListener;
        }

        public void Produce(object message, IEnumerable<MessageHeader> headers = null)
        {
            // TODO: 1. Not if diagnostics source isn't enabled.
            // 2. If current is empty, create a new activity.
            // 3. Ensure that there is no header with this name .
            var enrichedHeaders = new List<MessageHeader>(headers);

            if (_diagnosticListener.IsEnabled())
            {
                var activity = new Activity(ActivityName);
                _diagnosticListener.StartActivity(activity, new { message });

                enrichedHeaders.Add(new MessageHeader(CorrelationIdHeaderKey, activity.Id));
                enrichedHeaders.Add(BaggageToHeader(activity.Baggage));
                try
                {
                    //process request ...
                }
                finally
                {
                    //stop activity
                    _diagnosticListener.StopActivity(activity, new { message });
                }
            }

            GetMessageContentChunks(message)
                .ForEach(x => Produce(x.message, x.serializedMessage, enrichedHeaders));
        }

        // TODO: Move to Shared baggage class
        private MessageHeader BaggageToHeader(IEnumerable<KeyValuePair<string, string>> baggage)
        {
            return new MessageHeader();
            // TODO: Implement
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
            ILogger<Producer> logger, MessageLogger messageLogger, DiagnosticListener diagnosticListener)
            : base(broker, endpoint, messageKeyProvider, logger, messageLogger, diagnosticListener)
        {
        }

        protected new TBroker Broker => (TBroker)base.Broker;

        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}