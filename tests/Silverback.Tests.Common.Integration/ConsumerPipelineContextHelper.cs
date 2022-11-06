// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;

namespace Silverback.Tests;

public static class ConsumerPipelineContextHelper
{
    public static ConsumerPipelineContext CreateSubstitute(
        IRawInboundEnvelope? envelope = null,
        IServiceProvider? serviceProvider = null,
        IConsumerTransactionManager? transactionManager = null,
        IConsumer? consumer = null,
        ISequenceStore? sequenceStore = null,
        ConsumerEndpoint? endpoint = null,
        IBrokerMessageIdentifier? identifier = null,
        ISequence? sequence = null)
    {
        ConsumerPipelineContext context = new(
            envelope ?? new RawInboundEnvelope(
                Stream.Null,
                Array.Empty<MessageHeader>(),
                endpoint ?? TestConsumerEndpoint.GetDefault(),
                consumer ?? Substitute.For<IConsumer>(),
                identifier ?? new TestOffset()),
            consumer ?? Substitute.For<IConsumer>(),
            sequenceStore ?? Substitute.For<ISequenceStore>(),
            serviceProvider ?? GetServiceProvider())
        {
            TransactionManager = transactionManager ?? Substitute.For<IConsumerTransactionManager>()
        };

        if (sequence != null)
            context.SetSequence(sequence, true);

        return context;
    }

    private static IServiceProvider GetServiceProvider() =>
        ServiceProviderHelper.GetScopedServiceProvider(
            services =>
                services
                    .AddSingleton(Substitute.For<IHostApplicationLifetime>())
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker());
}
