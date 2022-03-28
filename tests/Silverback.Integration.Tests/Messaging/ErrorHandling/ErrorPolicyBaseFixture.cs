// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class ErrorPolicyBaseFixture
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]

    public static IEnumerable<object[]> IncludedExceptions_TestData =>
        new[]
        {
            new object[] { new ArgumentException(), true },
            new object[] { new ArgumentOutOfRangeException(), true },
            new object[] { new InvalidCastException(), true },
            new object[] { new FormatException(), false }
        };

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]
    public static IEnumerable<object[]> ExcludedException_TestData =>
        new[]
        {
            new object[] { new ArgumentException(), false },
            new object[] { new ArgumentOutOfRangeException(), false },
            new object[] { new InvalidCastException(), false },
            new object[] { new FormatException(), true }
        };

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]
    public static IEnumerable<object[]> IncludedAndExcludedExceptions_TestData =>
        new[]
        {
            new object[] { new ArgumentException(), true },
            new object[] { new ArgumentNullException(), true },
            new object[] { new ArgumentOutOfRangeException(), false },
            new object[] { new InvalidCastException(), false },
            new object[] { new FormatException(), true }
        };

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]
    public static IEnumerable<object[]> ApplyRule_TestData =>
        new[]
        {
            new object[]
            {
                new InboundEnvelope(
                    null,
                    Stream.Null,
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3") },
                    TestConsumerEndpoint.GetDefault(),
                    Substitute.For<IConsumer>(),
                    new TestOffset()),
                new ArgumentException(),
                true
            },
            new object[]
            {
                new InboundEnvelope(
                    null,
                    Stream.Null,
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "6") },
                    TestConsumerEndpoint.GetDefault(),
                    Substitute.For<IConsumer>(),
                    new TestOffset()),
                new ArgumentException(),
                false
            },
            new object[]
            {
                new InboundEnvelope(
                    null,
                    Stream.Null,
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3") },
                    TestConsumerEndpoint.GetDefault(),
                    Substitute.For<IConsumer>(),
                    new TestOffset()),
                new ArgumentException("no"),
                false
            }
        };

    [Theory]
    [MemberData(nameof(IncludedExceptions_TestData))]
    public void CanHandle_ShouldEvaluateExceptionType_WhenIncludedExceptionsAreSpecified(Exception exception, bool mustApply)
    {
        IErrorPolicyImplementation policy = new TestErrorPolicy
            {
                IncludedExceptions = new[] { typeof(ArgumentException), typeof(InvalidCastException) }
            }
            .Build(Substitute.For<IServiceProvider>());

        bool canHandle = policy.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    null,
                    Stream.Null,
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "99") },
                    TestConsumerEndpoint.GetDefault(),
                    Substitute.For<IConsumer>(),
                    new TestOffset())),
            exception);

        canHandle.Should().Be(mustApply);
    }

    [Theory]
    [MemberData(nameof(ExcludedException_TestData))]
    public void CanHandle_ShouldEvaluateExceptionType_WhenExcludedExceptionsAreSpecified(Exception exception, bool mustApply)
    {
        IErrorPolicyImplementation policy = new TestErrorPolicy
            {
                ExcludedExceptions = new[] { typeof(ArgumentException), typeof(InvalidCastException) }
            }
            .Build(Substitute.For<IServiceProvider>());

        bool canHandle = policy.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    null,
                    Stream.Null,
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "99") },
                    TestConsumerEndpoint.GetDefault(),
                    Substitute.For<IConsumer>(),
                    new TestOffset())),
            exception);

        canHandle.Should().Be(mustApply);
    }

    [Theory]
    [MemberData(nameof(IncludedAndExcludedExceptions_TestData))]
    public void CanHandle_ShouldEvaluateExceptionType_WhenIncludedAndExcludedExceptionsAreSpecified(Exception exception, bool mustApply)
    {
        IErrorPolicyImplementation policy = new TestErrorPolicy
            {
                IncludedExceptions = new[] { typeof(ArgumentException), typeof(FormatException) },
                ExcludedExceptions = new[] { typeof(ArgumentOutOfRangeException) }
            }
            .Build(Substitute.For<IServiceProvider>());

        bool canHandle = policy.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    null,
                    Stream.Null,
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "99") },
                    TestConsumerEndpoint.GetDefault(),
                    Substitute.For<IConsumer>(),
                    new TestOffset())),
            exception);

        canHandle.Should().Be(mustApply);
    }

    [Theory]
    [MemberData(nameof(ApplyRule_TestData))]
    public void CanHandle_ShouldEvaluateApplyRule(IInboundEnvelope envelope, Exception exception, bool mustApply)
    {
        IErrorPolicyImplementation policy = new TestErrorPolicy
            {
                ApplyRule = (inboundEnvelope, ex) =>
                    inboundEnvelope.Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts) <= 5 && ex.Message != "no"
            }
            .Build(Substitute.For<IServiceProvider>());

        bool canHandle = policy.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope),
            exception);

        canHandle.Should().Be(mustApply);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(7, false)]
    public void CanHandle_ShouldEvaluateMaxFailedAttempts(int failedAttempts, bool expectedResult)
    {
        InboundEnvelope envelope = new(
            Stream.Null,
            new[]
            {
                new MessageHeader(DefaultMessageHeaders.FailedAttempts, failedAttempts.ToString(CultureInfo.InvariantCulture))
            },
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        IErrorPolicyImplementation policy = new TestErrorPolicy
            {
                MaxFailedAttempts = 3
            }
            .Build(Substitute.For<IServiceProvider>());

        bool canHandle = policy.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope),
            new InvalidOperationException());

        canHandle.Should().Be(expectedResult);
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldPublishMessage()
    {
        IPublisher? publisher = Substitute.For<IPublisher>();
        ServiceProvider? serviceProvider = new ServiceCollection().AddScoped(_ => publisher)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        IErrorPolicyImplementation policy = new TestErrorPolicy
            {
                MessageToPublishFactory = (_, exception) => new TestEventTwo { Content = exception.Message }
            }
            .Build(serviceProvider);

        InboundEnvelope envelope = new(
            Stream.Null,
            new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3") },
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider),
            new TimeoutException("Exception message."));

        await publisher.Received().PublishAsync(Arg.Is<TestEventTwo>(message => message.Content == "Exception message."));
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldNotPublishMessage_WhenFactoryReturnsNull()
    {
        IPublisher? publisher = Substitute.For<IPublisher>();
        ServiceProvider? serviceProvider = new ServiceCollection().AddScoped(_ => publisher)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        IErrorPolicyImplementation policy = new TestErrorPolicy
            {
                MessageToPublishFactory = (_, _) => null
            }
            .Build(serviceProvider);

        InboundEnvelope envelope = new(
            Stream.Null,
            new[]
            {
                new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3")
            },
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider),
            new ArgumentNullException());

        await publisher.DidNotReceive().PublishAsync(Arg.Any<object>());
    }
}
