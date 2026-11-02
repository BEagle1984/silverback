// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Consuming;

public class PublisherConsumerBehaviorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task HandleAsync_ShouldExposeUnboundedSequenceAbortedWithError_WhenStreamSubscriberThrowsOperationCanceledException()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleStreamAsync));

        static async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            await foreach (TestEventOne dummy in stream)
            {
                // Not caused by the consumer stop (e.g. a timed out HTTP request)
                throw new TaskCanceledException("Unrelated timeout");
            }
        }

        TestConsumerEndpoint endpoint = new TestConsumerEndpointConfiguration("test") { AllowStreaming = true }.GetDefaultEndpoint();
        IConsumer consumer = Substitute.For<IConsumer>();
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            new InboundEnvelope<TestEventOne>(new TestEventOne(), Stream.Null, [], endpoint, consumer, new TestOffset()),
            serviceProvider,
            consumer: consumer,
            sequenceStore: new SequenceStore(new SilverbackLoggerSubstitute<SequenceStore>()));
        PublisherConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<PublisherConsumerBehavior>>());

        Task handling = behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None).AsTask();

        // The failure is rethrown pushing the message and the sequence is exposed to let the TransactionHandlerConsumerBehavior check why
        // it was aborted, since the error policies applied by the abort might have stopped the consumer, canceling the token
        await Should.ThrowAsync<TaskCanceledException>(handling.WaitAsync(Timeout));
        ISequence unboundedSequence = context.UnboundedSequence.ShouldNotBeNull();
        unboundedSequence.IsAborted.ShouldBeTrue();
        unboundedSequence.AbortReason.ShouldBe(SequenceAbortReason.Error);
    }
}
