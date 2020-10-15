// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Topics;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;

namespace Silverback.Testing
{
    /// <inheritdoc cref="ITestingHelper" />
    // TODO: Can use a single class for all brokers (e.g. awaiting both Kafka and Rabbit)?
    public class KafkaTestingHelper : ITestingHelper
    {
        private readonly IInMemoryTopicCollection _topics;

        private readonly IOutboxReader _outboxReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaTestingHelper"/> class.
        /// </summary>
        /// <param name="topics">
        ///    The <see cref="IInMemoryTopicCollection"/>.
        /// </param>
        /// <param name="outboxReader">
        ///    The <see cref="IOutboxReader"/>.
        /// </param>
        public KafkaTestingHelper(IInMemoryTopicCollection topics, IOutboxReader outboxReader)
        {
            _topics = topics;
            _outboxReader = outboxReader;
        }

        /// <inheritdoc cref="ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(TimeSpan?)" />
        public Task WaitUntilAllMessagesAreConsumedAsync(TimeSpan? timeout = null) =>
            WaitUntilAllMessagesAreConsumedAsync(null, timeout);

        /// <inheritdoc cref="ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(string[], TimeSpan?)" />
        Task ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(string[] topicNames, TimeSpan? timeout) =>
            WaitUntilAllMessagesAreConsumedAsync(topicNames, timeout);

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        [SuppressMessage("", "CA2000", Justification = Justifications.NewUsingSyntaxFalsePositive)]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The tasks are awaited")]
        private async Task WaitUntilAllMessagesAreConsumedAsync(string[]? topicNames, TimeSpan? timeout = null)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));

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
                // Ignore
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
