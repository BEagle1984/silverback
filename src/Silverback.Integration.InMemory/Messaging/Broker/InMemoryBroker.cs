// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation that is used for testing purpose only. The messages are not
    ///     transferred through a real message broker.
    /// </summary>
    public class InMemoryBroker : Broker<IProducerEndpoint, IConsumerEndpoint>
    {
        private readonly ConcurrentDictionary<string, InMemoryTopic> _topics =
            new ConcurrentDictionary<string, InMemoryTopic>();

        private readonly ConcurrentBag<InMemoryConsumer> _consumers = new ConcurrentBag<InMemoryConsumer>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryBroker" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public InMemoryBroker(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        /// <summary>
        ///     Resets the offset of all topics. The offset for the next messages will restart from 0. This can be
        ///     used to simulate the case when the very same messages are consumed once again.
        /// </summary>
        public void ResetOffsets() => _topics.Values.ForEach(topic => topic.ResetOffset());

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all messages routed to the consumers have been
        ///     processed.
        /// </summary>
        /// <param name="timeout">
        ///     The maximum waiting time after which the method will return even if the messages haven't been
        ///     processed. The default if not specified is 5 seconds.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all messages have been processed.
        /// </returns>
        [SuppressMessage("", "CA2000", Justification = Justifications.NewUsingSyntaxFalsePositive)]
        public async Task WaitUntilAllMessagesAreConsumed(TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromSeconds(5);

            using var cancellationTokenSource = new CancellationTokenSource(timeout.Value);

            try
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (_consumers.All(consumer => !consumer.IsConnected || consumer.IsQueueEmpty))
                        return;

                    await Task.Delay(50, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
        }

        internal InMemoryTopic GetTopic(string name) =>
            _topics.GetOrAdd(name, _ => new InMemoryTopic(name));

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateProducer" />
        protected override IProducer InstantiateProducer(
            IProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider) =>
            new InMemoryProducer(
                this,
                endpoint,
                behaviorsProvider,
                serviceProvider,
                serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<InMemoryProducer>>());

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateConsumer" />
        protected override IConsumer InstantiateConsumer(
            IConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider)
        {
            Check.NotNull(endpoint, nameof(endpoint));
            Check.NotNull(serviceProvider, nameof(serviceProvider));

            var consumer = new InMemoryConsumer(
                this,
                endpoint,
                behaviorsProvider,
                serviceProvider,
                serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<InMemoryConsumer>>());

            GetTopic(endpoint.Name).Subscribe(consumer);

            _consumers.Add(consumer);

            return consumer;
        }
    }
}
