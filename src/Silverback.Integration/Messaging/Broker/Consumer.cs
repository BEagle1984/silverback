// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public abstract class Consumer : EndpointConnectedObject, IConsumer
    {
        private readonly IEnumerable<IConsumerBehavior> _behaviors;

        protected Consumer(IBroker broker, IEndpoint endpoint, IEnumerable<IConsumerBehavior> behaviors)
            : base(broker, endpoint)
        {
            _behaviors = behaviors;
        }

        public event MessageReceivedHandler Received;

        public Task Acknowledge(IOffset offset) => Acknowledge(new[] {offset});

        public abstract Task Acknowledge(IEnumerable<IOffset> offsets);

        protected virtual async Task HandleMessage(byte[] message, IEnumerable<MessageHeader> headers, IOffset offset)
        {
            if (Received == null)
                throw new InvalidOperationException(
                    "A message was received but no handler is configured, please attach to the Received event.");

            await ExecutePipeline(
                _behaviors, 
                new RawBrokerMessage(message, headers, Endpoint, offset),
                m => Received.Invoke(this, new MessageReceivedEventArgs(m)));
        }
        
        private async Task ExecutePipeline(IEnumerable<IConsumerBehavior> behaviors, RawBrokerMessage message, RawBrokerMessageHandler finalAction)
        {
            if (behaviors != null && behaviors.Any())
            {
                await behaviors.First().Handle(message, m => ExecutePipeline(behaviors.Skip(1), m, finalAction));
            }
            else
            {
                await finalAction(message);
            }
        }
    }

    public abstract class Consumer<TBroker, TEndpoint> : Consumer
        where TBroker : class, IBroker
        where TEndpoint : class, IEndpoint
    {
        protected Consumer(IBroker broker, IEndpoint endpoint, IEnumerable<IConsumerBehavior> behaviors)
            : base(broker, endpoint, behaviors)
        {
        }

        protected new TBroker Broker => (TBroker)base.Broker;

        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}
