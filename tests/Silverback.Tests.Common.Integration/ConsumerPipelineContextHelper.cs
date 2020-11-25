// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Types;

namespace Silverback.Tests
{
    public static class ConsumerPipelineContextHelper
    {
        public static ConsumerPipelineContext CreateSubstitute(
            IRawInboundEnvelope? envelope = null,
            IServiceProvider? serviceProvider = null,
            IConsumerTransactionManager? transactionManager = null,
            IConsumer? consumer = null,
            ISequenceStore? sequenceStore = null,
            IConsumerEndpoint? endpoint = null) =>
            new(
                envelope ?? new RawInboundEnvelope(
                    Stream.Null,
                    Array.Empty<MessageHeader>(),
                    endpoint ?? new TestConsumerEndpoint("test"),
                    endpoint?.Name ?? "test",
                    new TestOffset()),
                consumer ?? Substitute.For<IConsumer>(),
                sequenceStore ?? Substitute.For<ISequenceStore>(),
                serviceProvider ?? GetServiceProvider())
            {
                TransactionManager = transactionManager ?? Substitute.For<IConsumerTransactionManager>()
            };

        private static IServiceProvider GetServiceProvider() =>
            ServiceProviderHelper.GetServiceProvider(
                services =>
                    services.AddSilverback().WithConnectionToMessageBroker());
    }
}
