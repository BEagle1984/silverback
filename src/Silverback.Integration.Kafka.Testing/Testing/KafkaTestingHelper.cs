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
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;

namespace Silverback.Testing
{
    /// <inheritdoc cref="IKafkaTestingHelper" />
    public class KafkaTestingHelper : TestingHelper<KafkaBroker>, IKafkaTestingHelper
    {
        private readonly IInMemoryTopicCollection? _topics;

        private readonly ISilverbackIntegrationLogger<KafkaTestingHelper> _logger;

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
            ISilverbackIntegrationLogger<KafkaTestingHelper> logger)
            : base(serviceProvider)
        {
            _topics = serviceProvider.GetService<IInMemoryTopicCollection>();
            _logger = logger;
        }

        /// <inheritdoc cref="IKafkaTestingHelper.GetTopic" />
        public IInMemoryTopic GetTopic(string name) =>
            _topics?[name] ??
            throw new InvalidOperationException("The IInMemoryTopicCollection is not initialized.");

        /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilAllMessagesAreConsumedAsync(TimeSpan?)" />
        public override Task WaitUntilAllMessagesAreConsumedAsync(TimeSpan? timeout = null) =>
            WaitUntilAllMessagesAreConsumedCoreAsync(null, timeout);

        /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync(IReadOnlyCollection{string}, TimeSpan?)" />
        public Task WaitUntilAllMessagesAreConsumedAsync(
            IReadOnlyCollection<string> topicNames,
            TimeSpan? timeout = null) =>
            WaitUntilAllMessagesAreConsumedCoreAsync(topicNames, timeout);

        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The tasks are awaited")]
        private async Task WaitUntilAllMessagesAreConsumedCoreAsync(
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
                                    topic.WaitUntilAllMessagesAreConsumedAsync(
                                        cancellationTokenSource.Token)))
                        .ConfigureAwait(false);
                }
                while (await OutboxReader.GetLengthAsync().ConfigureAwait(false) > 0);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "The timeout elapsed before all messages could be consumed and processed.");
            }
        }
    }
}
