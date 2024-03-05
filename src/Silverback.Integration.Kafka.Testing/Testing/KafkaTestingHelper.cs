// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;

namespace Silverback.Testing
{
    /// <inheritdoc cref="IKafkaTestingHelper" />
    public class KafkaTestingHelper : TestingHelper<KafkaBroker>, IKafkaTestingHelper
    {
        private readonly InMemoryTopicCollection? _topics;

        private readonly KafkaBroker _kafkaBroker;

        private readonly ILogger<KafkaTestingHelper> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaTestingHelper" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public KafkaTestingHelper(
            IServiceProvider serviceProvider,
            ILogger<KafkaTestingHelper> logger)
            : base(serviceProvider, logger)
        {
            _topics = serviceProvider.GetService<InMemoryTopicCollection>();
            _kafkaBroker = serviceProvider.GetRequiredService<KafkaBroker>();
            _logger = logger;
        }

        /// <inheritdoc cref="IKafkaTestingHelper.GetTopic(string)" />
        public IInMemoryTopic GetTopic(string name) =>
            GetTopics(name).First();

        /// <inheritdoc cref="IKafkaTestingHelper.GetTopic(string,string)" />
        public IInMemoryTopic GetTopic(string name, string bootstrapServers) =>
            GetTopics(name, bootstrapServers).First();

        /// <inheritdoc cref="IKafkaTestingHelper.GetTopics" />
        public IReadOnlyCollection<IInMemoryTopic> GetTopics(string name, string? bootstrapServers = null)
        {
            if (_topics == null)
                throw new InvalidOperationException("The IInMemoryTopicCollection is not initialized.");

            List<IInMemoryTopic> topics = _topics.Where(
                    topic =>
                        topic.Name == name &&
                        (bootstrapServers == null || string.Equals(
                            bootstrapServers,
                            topic.BootstrapServers,
                            StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (topics.Count != 0)
                return topics;

            if (bootstrapServers != null)
                return new[] { _topics.Get(name, bootstrapServers) };

            // If the topic wasn't created yet, just create one per each broker
            return _kafkaBroker
                .Producers.Select(producer => ((KafkaProducerEndpoint)producer.Endpoint).Configuration.BootstrapServers)
                .Union(
                    _kafkaBroker.Consumers.Select(
                        consumer =>
                            ((KafkaConsumerEndpoint)consumer.Endpoint).Configuration.BootstrapServers))
                .Select(servers => servers.ToUpperInvariant())
                .Distinct()
                .Select(servers => _topics.Get(name, servers))
                .ToList();
        }

        /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilAllMessagesAreConsumedAsync(bool,TimeSpan?)" />
        public override Task WaitUntilAllMessagesAreConsumedAsync(
            bool throwTimeoutException,
            TimeSpan? timeout = null) =>
            WaitUntilAllMessagesAreConsumedCoreAsync(throwTimeoutException, null, timeout);

        /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync(IReadOnlyCollection{string}, TimeSpan?)" />
        public Task WaitUntilAllMessagesAreConsumedAsync(
            IReadOnlyCollection<string> topicNames,
            TimeSpan? timeout = null) =>
            WaitUntilAllMessagesAreConsumedAsync(true, topicNames, timeout);

        /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync(IReadOnlyCollection{string}, TimeSpan?)" />
        public Task WaitUntilAllMessagesAreConsumedAsync(
            bool throwTimeoutException,
            IReadOnlyCollection<string> topicNames,
            TimeSpan? timeout = null) =>
            WaitUntilAllMessagesAreConsumedCoreAsync(true, topicNames, timeout);

        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The tasks are awaited")]
        private async Task WaitUntilAllMessagesAreConsumedCoreAsync(
            bool throwTimeoutException,
            IReadOnlyCollection<string>? topicNames,
            TimeSpan? timeout = null)
        {
            if (_topics == null)
                return;

            using var cancellationTokenSource =
                new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));

            var topics = topicNames == null
                ? _topics.ToList()
                : _topics.Where(topic => topicNames.Contains(topic.Name)).ToList();

            try
            {
                // Loop until the outbox is empty since the consumers may produce new messages
                do
                {
                    await WaitUntilOutboxIsEmptyAsync(cancellationTokenSource.Token).ConfigureAwait(false);

                    await Task.WhenAll(
                            topics.Select(
                                topic =>
                                    topic.WaitUntilAllMessagesAreConsumedAsync(cancellationTokenSource.Token)))
                        .ConfigureAwait(false);
                }
                while (!await IsOutboxEmptyAsync().ConfigureAwait(false));
            }
            catch (OperationCanceledException)
            {
                const string message =
                    "The timeout elapsed before all messages could be consumed and processed.";

                if (throwTimeoutException)
                    throw new TimeoutException(message);

                _logger.LogWarning(message);
            }
        }
    }
}
