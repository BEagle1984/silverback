// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Testing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>UseManualOutboxWorker</c> and <c>BypassOutbox</c> methods to the <see cref="BrokerOptionsBuilder" />.
/// </summary>
public static class BrokerOptionsBuilderOutboxTestingExtensions
{
    /// <summary>
    ///     Prevents the registered outbox workers from processing the outbox on their own. The tests are in control and can trigger the
    ///     processing calling <see cref="IOutboxTestingHelper.ProcessAsync(System.Threading.CancellationToken)" /> (via
    ///     <see cref="ITestingHelper.Outbox" />).
    /// </summary>
    /// <remarks>
    ///     The workers are suspended like with <see cref="IOutboxTestingHelper.PauseWorkerAsync(System.Threading.CancellationToken)" />,
    ///     so <see cref="IOutboxTestingHelper.ResumeWorker()" /> switches them back to the regular processing.
    /// </remarks>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" />.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder UseManualOutboxWorker(this BrokerOptionsBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        // The host calls StartingAsync of all the IHostedLifecycleService before starting any service, so the workers are suspended
        // before they run, regardless of the registration order
        builder.SilverbackBuilder.Services.AddSingleton<IHostedService, ManualOutboxWorkerLifecycleService>();

        return builder;
    }

    /// <summary>
    ///     Replaces the outbox writer with one that produces the messages to the message broker right away, instead of storing them to the
    ///     outbox. No outbox worker is needed and the outbox remains empty.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The database transaction (if any) is ignored and the produce errors are thrown to the publisher.
    ///     </para>
    ///     <para>
    ///         The outbox reader is replaced as well, with one that is always empty, so that no outbox storage is required.
    ///     </para>
    ///     <para>
    ///         This method must be called inside the <c>WithConnectionToMessageBroker</c> callback, after any other method overriding the
    ///         outbox (e.g. <c>UseInMemoryOutbox</c>), since the last override wins.
    ///     </para>
    /// </remarks>
    /// <param name="builder">
    ///     The <see cref="BrokerOptionsBuilder" />.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder BypassOutbox(this BrokerOptionsBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        OutboxReaderFactory? readerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxReaderFactory>();
        OutboxWriterFactory? writerFactory = builder.SilverbackBuilder.Services.GetSingletonServiceInstance<OutboxWriterFactory>();

        if (readerFactory == null || writerFactory == null)
            throw new InvalidOperationException("OutboxReaderFactory/OutboxWriterFactory not found, WithConnectionToMessageBroker has not been called.");

        readerFactory.OverrideFactories((_, _) => new EmptyOutboxReader());
        writerFactory.OverrideFactories((_, serviceProvider) => new BypassOutboxWriter(serviceProvider.GetRequiredService<IProducerCollection>()));

        return builder;
    }
}
