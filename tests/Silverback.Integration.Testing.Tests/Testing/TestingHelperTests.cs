// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Testing;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Testing.Testing;

public class TestingHelperTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task WaitUntilConnectedAsync_ShouldThrowTimeoutException_WhenConsumerDoesNotConnectWithinTimeout()
    {
        TestingHelper testingHelper = CreateTestingHelper(ConsumerStatus.Started);

        TimeoutException exception = await Should.ThrowAsync<TimeoutException>(
            testingHelper.WaitUntilConnectedAsync(true, WaitTimeout).AsTask().WaitAsync(TestTimeout));

        exception.Message.ShouldBe("Timeout elapsed before the consumers successfully established a connection");
    }

    [Fact]
    public async Task WaitUntilConnectedAsync_ShouldNotThrow_WhenConsumerDoesNotConnectWithinTimeoutAndThrowTimeoutExceptionIsFalse()
    {
        TestingHelper testingHelper = CreateTestingHelper(ConsumerStatus.Started);

        await testingHelper.WaitUntilConnectedAsync(false, WaitTimeout).AsTask().WaitAsync(TestTimeout);
    }

    private static TestingHelper CreateTestingHelper(ConsumerStatus consumerStatus)
    {
        IConsumerStatusInfo statusInfo = Substitute.For<IConsumerStatusInfo>();
        statusInfo.Status.Returns(consumerStatus);
        IConsumer consumer = Substitute.For<IConsumer>();
        consumer.StatusInfo.Returns(statusInfo);

        ServiceProvider serviceProvider = new ServiceCollection()
            .AddSingleton<IConsumerCollection>(new ConsumerCollection { consumer })
            .BuildServiceProvider();

        return new TestTestingHelper(serviceProvider, new LoggerSubstitute<TestingHelper>(LogLevel.Trace));
    }

    private sealed class TestTestingHelper : TestingHelper
    {
        public TestTestingHelper(IServiceProvider serviceProvider, ILogger<TestingHelper> logger)
            : base(serviceProvider, logger)
        {
        }

        protected override IProducer GetProducerForConsumer(IConsumerCollection consumers, string endpointName) =>
            throw new NotSupportedException();

        protected override Task WaitUntilAllMessagesAreConsumedCoreAsync(
            IReadOnlyCollection<string> endpointNames,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
