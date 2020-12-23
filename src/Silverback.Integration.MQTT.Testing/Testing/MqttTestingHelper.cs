// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Mqtt.Mocks;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;

namespace Silverback.Testing
{
    /// <inheritdoc cref="IMqttTestingHelper" />
    public class MqttTestingHelper : IMqttTestingHelper
    {
        private readonly IInMemoryMqttBroker _broker;

        private readonly IOutboxReader _outboxReader;

        private readonly ISilverbackIntegrationLogger<MqttTestingHelper> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttTestingHelper" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IInMemoryMqttBroker" />.
        /// </param>
        /// <param name="outboxReader">
        ///     The <see cref="IOutboxReader" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public MqttTestingHelper(
            IInMemoryMqttBroker broker,
            IOutboxReader outboxReader,
            ISilverbackIntegrationLogger<MqttTestingHelper> logger)
        {
            _broker = broker;
            _outboxReader = outboxReader;
            _logger = logger;
        }

        /// <inheritdoc cref="IMqttTestingHelper.WaitUntilAllMessagesAreConsumedAsync(TimeSpan?)" />
        public async Task WaitUntilAllMessagesAreConsumedAsync(TimeSpan? timeout = null)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));

            try
            {
                do
                {
                    await WaitUntilOutboxIsEmptyAsync(cancellationTokenSource.Token).ConfigureAwait(false);

                    await _broker.WaitUntilAllMessagesAreConsumedAsync(cancellationTokenSource.Token)
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
