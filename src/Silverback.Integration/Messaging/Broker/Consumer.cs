// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public abstract class Consumer : EndpointConnectedObject, IConsumer
    {
        private const string DiagnosticListenerName = "SilverbackConsumerDiagnosticListener";
        private const string ActivityName = "Silverback.Messaging.Broker.Consumer.ConsumeMessage";
        private const string CorrelationIdHeaderKey = "CorrelationId"; // TODO: Config.
        private const string CorrelationBaggageHeaderKey = "Correlation-Context"; // TODO: Config.

        private readonly ILogger<Consumer> _logger;
        private readonly MessageLogger _messageLogger;
        private readonly DiagnosticListener _diagnosticListener;

        protected Consumer(IBroker broker, IEndpoint endpoint, ILogger<Consumer> logger, MessageLogger messageLogger, DiagnosticListener diagnosticListener)
           : base(broker, endpoint)
        {
            _logger = logger;
            _messageLogger = messageLogger;
            _diagnosticListener = diagnosticListener;
        }

        public event EventHandler<MessageReceivedEventArgs> Received;

        public void Acknowledge(IOffset offset)
        {
            Acknowledge(new[] { offset });
        }

        public abstract void Acknowledge(IEnumerable<IOffset> offsets);

        protected void HandleMessage(byte[] message, IEnumerable<MessageHeader> headers, IOffset offset)
        {
            Activity activity = StartActivity(headers);

            if (Received == null)
                throw new InvalidOperationException("A message was received but no handler is configured, please attach to the Received event.");

            var deserializedMessage = Endpoint.Serializer.Deserialize(message);

            _messageLogger.LogTrace(_logger, "Message received.", deserializedMessage, Endpoint);

            Received.Invoke(this, new MessageReceivedEventArgs(deserializedMessage, headers, offset));

            StopActivity(activity);
        }

        private Activity StartActivity(IEnumerable<MessageHeader> messageHeaders)
        {
            string correlationId =  messageHeaders.GetFromHeaders(CorrelationIdHeaderKey);

            var activity = new Activity(ActivityName);
            if (!string.IsNullOrEmpty(correlationId))
            {
                // This will reflect, that the current activity is a child of the activity
                // which is represented in the message.
                activity.SetParentId(correlationId);

                // We expect baggage to be empty by default
                // Only very advanced users will be using it in near future, we encourage them to keep baggage small (few items)
                string baggage = messageHeaders.GetFromHeaders(CorrelationBaggageHeaderKey);
                if (BaggageParser.TryParse(baggage, out var baggageItems))
                {
                    foreach (KeyValuePair<string, string> baggageItem in baggageItems)
                    {
                        activity.AddBaggage(baggageItem.Key, baggageItem.Value);
                    }
                }
            }

            if (_diagnosticListener.IsEnabled(DiagnosticListenerName))
            {
                _diagnosticListener.StartActivity(activity, null);
            }
            else
            {
                activity.Start();
            }

            return activity;
        }

        private void StopActivity(Activity activity)
        {
            _diagnosticListener.StopActivity(activity, null);
        }
    }

    public abstract class Consumer<TBroker, TEndpoint> : Consumer
        where TBroker : class, IBroker
        where TEndpoint : class, IEndpoint
    {
        protected Consumer(IBroker broker, IEndpoint endpoint,
            ILogger<Consumer> logger, MessageLogger messageLogger, DiagnosticListener diagnosticListener)
            : base(broker, endpoint, logger, messageLogger, diagnosticListener)
        {
        }

        protected new TBroker Broker => (TBroker)base.Broker;

        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}
