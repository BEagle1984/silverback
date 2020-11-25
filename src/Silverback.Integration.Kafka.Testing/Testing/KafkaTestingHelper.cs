// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Topics;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;

namespace Silverback.Testing
{
    /// <inheritdoc cref="IKafkaTestingHelper" />
    public class KafkaTestingHelper : IKafkaTestingHelper
    {
        private readonly IInMemoryTopicCollection _topics;

        private readonly IOutboxReader _outboxReader;

        private readonly ISilverbackIntegrationLogger<KafkaTestingHelper> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaTestingHelper" /> class.
        /// </summary>
        /// <param name="topics">
        ///     The <see cref="IInMemoryTopicCollection" />.
        /// </param>
        /// <param name="outboxReader">
        ///     The <see cref="IOutboxReader" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public KafkaTestingHelper(
            IInMemoryTopicCollection topics,
            IOutboxReader outboxReader,
            ISilverbackIntegrationLogger<KafkaTestingHelper> logger)
        {
            _topics = topics;
            _outboxReader = outboxReader;
            _logger = logger;
        }

        /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync(TimeSpan?)" />
        public Task WaitUntilAllMessagesAreConsumedAsync(TimeSpan? timeout = null) =>
            WaitUntilAllMessagesAreConsumedAsync(null, timeout);

        /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync(string[], TimeSpan?)" />
        Task IKafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync(string[] topicNames, TimeSpan? timeout) =>
            WaitUntilAllMessagesAreConsumedAsync(topicNames, timeout);

        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The tasks are awaited")]
        private async Task WaitUntilAllMessagesAreConsumedAsync(string[]? topicNames, TimeSpan? timeout = null)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));

            var topics = topicNames == null
                ? _topics.ToList()
                : _topics.Where(topic => topicNames.Contains(topic.Name)).ToList();

            try
            {
                do
                {
                    await WaitUntilOutboxIsEmptyAsync(cancellationTokenSource.Token).ConfigureAwait(false);

                    await Task.WhenAll(
                            topics.Select(
                                topic =>
                                    topic.WaitUntilAllMessagesAreConsumedAsync(cancellationTokenSource.Token)))
                        .ConfigureAwait(false);
                }
                while (await _outboxReader.GetLengthAsync().ConfigureAwait(false) > 0); // Loop until the outbox is empty since the consumers may produce new messages
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("The timeout elapsed before all messages could be consumed and processed.");
            }
        }

        private async Task WaitUntilOutboxIsEmptyAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (await _outboxReader.GetLengthAsync().ConfigureAwait(false) == 0)
                    return;

                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
