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
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class ErrorPolicyBaseTests
    {
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
        [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]

        public static IEnumerable<object[]> ApplyTo_TestData =>
            new[]
            {
                new object[] { new ArgumentException(), true },
                new object[] { new ArgumentOutOfRangeException(), true },
                new object[] { new InvalidCastException(), true },
                new object[] { new FormatException(), false }
            };

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
        [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]
        public static IEnumerable<object[]> Exclude_TestData =>
            new[]
            {
                new object[] { new ArgumentException(), false },
                new object[] { new ArgumentOutOfRangeException(), false },
                new object[] { new InvalidCastException(), false },
                new object[] { new FormatException(), true }
            };

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
        [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]
        public static IEnumerable<object[]> ApplyToAndExclude_TestData =>
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
        public static IEnumerable<object[]> ApplyWhen_TestData =>
            new[]
            {
                new object[]
                {
                    new InboundEnvelope(
                        new MemoryStream(),
                        new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3") },
                        new TestOffset(),
                        TestConsumerEndpoint.GetDefault(),
                        TestConsumerEndpoint.GetDefault().Name),
                    new ArgumentException(),
                    true
                },
                new object[]
                {
                    new InboundEnvelope(
                        new MemoryStream(),
                        new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "6") },
                        new TestOffset(),
                        TestConsumerEndpoint.GetDefault(),
                        TestConsumerEndpoint.GetDefault().Name),
                    new ArgumentException(),
                    false
                },
                new object[]
                {
                    new InboundEnvelope(
                        new MemoryStream(),
                        new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3") },
                        new TestOffset(),
                        TestConsumerEndpoint.GetDefault(),
                        TestConsumerEndpoint.GetDefault().Name),
                    new ArgumentException("no"),
                    false
                }
            };

        [Theory]
        [MemberData(nameof(ApplyTo_TestData))]
        public void CanHandle_WithApplyTo_ExpectedResultReturned(Exception exception, bool mustApply)
        {
            var policy = new TestErrorPolicy()
                .ApplyTo<ArgumentException>()
                .ApplyTo<InvalidCastException>()
                .Build(Substitute.For<IServiceProvider>());

            var canHandle = policy.CanHandle(
                ConsumerPipelineContextHelper.CreateSubstitute(
                    new InboundEnvelope(
                        new MemoryStream(),
                        new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "99") },
                        new TestOffset(),
                        TestConsumerEndpoint.GetDefault(),
                        TestConsumerEndpoint.GetDefault().Name)),
                exception);

            canHandle.Should().Be(mustApply);
        }

        [Theory]
        [MemberData(nameof(Exclude_TestData))]
        public void CanHandle_WithExclude_ExpectedResultReturned(Exception exception, bool mustApply)
        {
            var policy = new TestErrorPolicy()
                .Exclude<ArgumentException>()
                .Exclude<InvalidCastException>()
                .Build(Substitute.For<IServiceProvider>());

            var canHandle = policy.CanHandle(
                ConsumerPipelineContextHelper.CreateSubstitute(
                    new InboundEnvelope(
                        new MemoryStream(),
                        new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "99") },
                        new TestOffset(),
                        TestConsumerEndpoint.GetDefault(),
                        TestConsumerEndpoint.GetDefault().Name)),
                exception);

            canHandle.Should().Be(mustApply);
        }

        [Theory]
        [MemberData(nameof(ApplyToAndExclude_TestData))]
        public void CanHandle_WithApplyToAndExclude_ExpectedResultReturned(
            Exception exception,
            bool mustApply)
        {
            var policy = new TestErrorPolicy()
                .ApplyTo<ArgumentException>()
                .Exclude<ArgumentOutOfRangeException>()
                .ApplyTo<FormatException>()
                .Build(Substitute.For<IServiceProvider>());

            var canHandle = policy.CanHandle(
                ConsumerPipelineContextHelper.CreateSubstitute(
                    new InboundEnvelope(
                        new MemoryStream(),
                        new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "99") },
                        new TestOffset(),
                        TestConsumerEndpoint.GetDefault(),
                        TestConsumerEndpoint.GetDefault().Name)),
                exception);

            canHandle.Should().Be(mustApply);
        }

        [Theory]
        [MemberData(nameof(ApplyWhen_TestData))]
        public void CanHandle_WithApplyWhen_ExpectedResultReturned(
            IInboundEnvelope envelope,
            Exception exception,
            bool mustApply)
        {
            var policy = new TestErrorPolicy()
                .ApplyWhen(
                    (msg, ex) =>
                        msg.Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts) <= 5 &&
                        ex.Message != "no")
                .Build(Substitute.For<IServiceProvider>());

            var canHandle = policy.CanHandle(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope),
                exception);

            canHandle.Should().Be(mustApply);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(7, false)]
        public void CanHandle_WithMaxFailedAttempts_ExpectedResultReturned(
            int failedAttempts,
            bool expectedResult)
        {
            var envelope = new InboundEnvelope(
                new MemoryStream(),
                new[]
                {
                    new MessageHeader(
                        DefaultMessageHeaders.FailedAttempts,
                        failedAttempts.ToString(CultureInfo.InvariantCulture))
                },
                new TestOffset(),
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            var policy = new TestErrorPolicy()
                .MaxFailedAttempts(3)
                .Build(Substitute.For<IServiceProvider>());

            var canHandle = policy.CanHandle(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope),
                new InvalidOperationException());

            canHandle.Should().Be(expectedResult);
        }

        [Fact]
        public async Task HandleErrorAsync_WithPublish_MessagePublished()
        {
            var publisher = Substitute.For<IPublisher>();
            var serviceProvider = new ServiceCollection().AddScoped(_ => publisher)
                .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            var policy = new TestErrorPolicy()
                .Publish(_ => new TestEventTwo { Content = "aaa" })
                .Build(serviceProvider);

            var envelope = new InboundEnvelope(
                new MemoryStream(),
                new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3") },
                new TestOffset(),
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider),
                new ArgumentNullException());

            await publisher.Received().PublishAsync(Arg.Any<TestEventTwo>());
        }

        [Fact]
        public async Task HandleErrorAsync_WithPublishReturningNull_NoMessagePublished()
        {
            var publisher = Substitute.For<IPublisher>();
            var serviceProvider = new ServiceCollection().AddScoped(_ => publisher)
                .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            var policy = new TestErrorPolicy()
                .Publish(_ => null)
                .Build(serviceProvider);

            var envelope = new InboundEnvelope(
                new MemoryStream(),
                new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3") },
                new TestOffset(),
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider),
                new ArgumentNullException());

            await publisher.DidNotReceive().PublishAsync(Arg.Any<object>());
        }

        // TODO: Test with multiple messages (batch) --> TODO2: Do we still need to?
    }
}
