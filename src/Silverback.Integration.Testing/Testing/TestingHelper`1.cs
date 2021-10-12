// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Util;

namespace Silverback.Testing
{
    /// <inheritdoc cref="ITestingHelper{TBroker}" />
    public abstract class TestingHelper<TBroker> : ITestingHelper<TBroker>
        where TBroker : IBroker
    {
        private readonly ILogger _logger;

        private readonly IServiceProvider _serviceProvider;

        private readonly TBroker _broker;

        private readonly IIntegrationSpy? _integrationSpy;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TestingHelper{TBroker}" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ILogger{TCategoryName}" />.
        /// </param>
        protected TestingHelper(IServiceProvider serviceProvider, ILogger<TestingHelper<TBroker>> logger)
        {
            _logger = logger;
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));

            _broker = serviceProvider.GetRequiredService<TBroker>();
            _integrationSpy = serviceProvider.GetService<IIntegrationSpy>();
        }

        /// <inheritdoc cref="ITestingHelper{TBroker}.Broker" />
        public TBroker Broker => _broker ?? throw new InvalidOperationException(
            $"No broker of type {typeof(TBroker).Name} could be resolved.");

        /// <inheritdoc cref="ITestingHelper{TBroker}.Spy" />
        public IIntegrationSpy Spy => _integrationSpy ?? throw new InvalidOperationException(
            "The IIntegrationSpy couldn't be resolved. " +
            "Register it calling AddIntegrationSpy or AddIntegrationSpyAndSubscriber.");

        /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilConnectedAsync" />
        public async Task WaitUntilConnectedAsync(TimeSpan? timeout = null)
        {
            using var cancellationTokenSource =
                new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));

            try
            {
                while (!_broker.IsConnected || _broker.Consumers.Any(
                    consumer => consumer.StatusInfo.Status < ConsumerStatus.Ready))
                {
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    await Task.Delay(10, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "The timeout elapsed before the consumers successfully established a connection.");
            }
        }

        /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilAllMessagesAreConsumedAsync(TimeSpan?)" />
        public abstract Task WaitUntilAllMessagesAreConsumedAsync(TimeSpan? timeout = null);

        /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilOutboxIsEmptyAsync" />
        public async Task WaitUntilOutboxIsEmptyAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (await IsOutboxEmptyAsync().ConfigureAwait(false))
                    return;

                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="ITestingHelper{TBroker}.IsOutboxEmptyAsync" />
        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        public async Task<bool> IsOutboxEmptyAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outboxReader = scope.ServiceProvider.GetRequiredService<IOutboxReader>();
                return await outboxReader.GetLengthAsync().ConfigureAwait(false) == 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An error occurred while checking whether the Outbox is empty.");

                return false;
            }
        }
    }
}
