// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public abstract class Consumer : IConsumer
    {
        private readonly IEnumerable<IConsumerBehavior> _behaviors;

        protected Consumer(IBroker broker, IConsumerEndpoint endpoint, IEnumerable<IConsumerBehavior> behaviors)
        {
            _behaviors = behaviors;

            Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            Endpoint.Validate();
        }

        /// <summary>
        /// Gets the <see cref="IBroker"/> instance that owns this instance.
        /// </summary>
        public IBroker Broker { get; }

        /// <summary>
        /// Gets the <see cref="IConsumerEndpoint"/> this instance is connected to.
        /// </summary>
        public IConsumerEndpoint Endpoint { get; }
        
        /// <inheritdoc cref="IConsumer"/>
        public event MessageReceivedHandler Received;

        /// <inheritdoc cref="IConsumer"/>
        public Task Acknowledge(IOffset offset) => Acknowledge(new[] {offset});
       
        /// <inheritdoc cref="IConsumer"/>
        public abstract Task Acknowledge(IEnumerable<IOffset> offsets);
        
        /// <inheritdoc cref="IConsumer"/>
        public abstract void Connect();
        
        /// <inheritdoc cref="IConsumer"/>
        public abstract void Disconnect();

        /// <summary>
        /// Handles the consumed message invoking the <see cref="IConsumerBehavior"/> pipeline and finally
        /// firing the <see cref="Received"/> event.
        /// </summary>
        /// <param name="message">The body of the consumed message.</param>
        /// <param name="headers">The headers of the consumed message.</param>
        /// <param name="offset">The offset of the consumed message.</param>
        /// <returns></returns>
        protected virtual async Task HandleMessage(byte[] message, IEnumerable<MessageHeader> headers, IOffset offset)
        {
            if (Received == null)
                throw new InvalidOperationException(
                    "A message was received but no handler is configured, please attach to the Received event.");

            await ExecutePipeline(
                _behaviors, 
                new RawInboundMessage(message, headers, Endpoint, offset), 
                m => Received.Invoke(this, new MessageReceivedEventArgs(m)));
        }
        
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
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
        where TBroker : IBroker
        where TEndpoint : IConsumerEndpoint
    {
        protected Consumer(TBroker broker, TEndpoint endpoint, IEnumerable<IConsumerBehavior> behaviors)
            : base(broker, endpoint, behaviors)
        {
        }

        /// <summary>
        /// Gets the <typeparamref name="TBroker"/> instance that owns this instance.
        /// </summary>
        protected new TBroker Broker => (TBroker)base.Broker;

        /// <summary>
        /// Gets the <see cref="IProducerEndpoint"/> this instance is connected to.
        /// </summary>
        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}
