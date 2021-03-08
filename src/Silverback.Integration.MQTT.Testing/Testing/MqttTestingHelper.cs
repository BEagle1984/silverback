// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt.Mocks;

namespace Silverback.Testing
{
    /// <inheritdoc cref="IMqttTestingHelper" />
    public class MqttTestingHelper : TestingHelper<MqttBroker>, IMqttTestingHelper
    {
        private readonly IInMemoryMqttBroker? _inMemoryMqttBroker;

        private readonly ILogger<MqttTestingHelper> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttTestingHelper" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public MqttTestingHelper(
            IServiceProvider serviceProvider,
            ILogger<MqttTestingHelper> logger)
            : base(serviceProvider, logger)
        {
            _inMemoryMqttBroker = serviceProvider.GetService<IInMemoryMqttBroker>();
            _logger = logger;
        }

        /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilAllMessagesAreConsumedAsync(TimeSpan?)" />
        public override async Task WaitUntilAllMessagesAreConsumedAsync(TimeSpan? timeout = null)
        {
            if (_inMemoryMqttBroker == null)
                return;

            using var cancellationTokenSource =
                new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));

            try
            {
                // Loop until the outbox is empty since the consumers may produce new messages
                do
                {
                    await WaitUntilOutboxIsEmptyAsync(cancellationTokenSource.Token).ConfigureAwait(false);

                    await _inMemoryMqttBroker
                        .WaitUntilAllMessagesAreConsumedAsync(cancellationTokenSource.Token)
                        .ConfigureAwait(false);
                }
                while (!await IsOutboxEmptyAsync().ConfigureAwait(false));
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "The timeout elapsed before all messages could be consumed and processed.");
            }
        }

        /// <inheritdoc cref="IMqttTestingHelper.GetClientSession" />
        public IClientSession GetClientSession(string clientId)
        {
            if (_inMemoryMqttBroker == null)
                throw new InvalidOperationException("The IInMemoryMqttBroker is not initialized.");

            return _inMemoryMqttBroker.GetClientSession(clientId);
        }

        /// <inheritdoc cref="IMqttTestingHelper.GetMessages" />
        public IReadOnlyList<MqttApplicationMessage> GetMessages(string topic)
        {
            if (_inMemoryMqttBroker == null)
                throw new InvalidOperationException("The IInMemoryMqttBroker is not initialized.");

            return _inMemoryMqttBroker.GetMessages(topic);
        }
    }
}
