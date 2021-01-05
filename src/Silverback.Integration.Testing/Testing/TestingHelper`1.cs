// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Util;

namespace Silverback.Testing
{
    /// <inheritdoc cref="ITestingHelper{TBroker}" />
    public abstract class TestingHelper<TBroker> : ITestingHelper<TBroker>
        where TBroker : IBroker
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly TBroker? _broker;

        private readonly IIntegrationSpy? _integrationSpy;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TestingHelper{TBroker}" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        protected TestingHelper(IServiceProvider serviceProvider)
        {
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));

            _broker = serviceProvider.GetRequiredService<TBroker>();
            _integrationSpy = serviceProvider.GetService<IIntegrationSpy>();
        }

        /// <inheritdoc cref="ITestingHelper{TBroker}.Broker" />
        [SuppressMessage("", "CA1508", Justification = "False positive")]
        public TBroker Broker => _broker ?? throw new InvalidOperationException(
            $"No broker of type {typeof(TBroker).Name} could be resolved.");

        /// <inheritdoc cref="ITestingHelper{TBroker}.Spy" />
        public IIntegrationSpy Spy => _integrationSpy ?? throw new InvalidOperationException(
            "The IIntegrationSpy couldn't be resolved. " +
            "Register it calling AddIntegrationSpy or AddIntegrationSpyAndSubscriber.");

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
        public async Task<bool> IsOutboxEmptyAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var outboxReader = scope.ServiceProvider.GetRequiredService<IOutboxReader>();
            return await outboxReader.GetLengthAsync().ConfigureAwait(false) == 0;
        }
    }
}
