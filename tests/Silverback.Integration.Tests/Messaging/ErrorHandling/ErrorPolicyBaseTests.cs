// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ErrorPolicyBaseTests
    {
        [Theory, MemberData(nameof(ApplyTo_TestData))]
        public void ApplyTo_Exception_CanHandleReturnsExpectedResult(Exception exception, bool mustApply)
        {
            var policy = new TestErrorPolicy()
                .ApplyTo<ArgumentException>()
                .ApplyTo<InvalidCastException>();

            var canHandle = policy.CanHandle(
                new InboundEnvelope(
                    new byte[1],
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "99") },
                    null, TestConsumerEndpoint.GetDefault(),
                    TestConsumerEndpoint.GetDefault().Name),
                exception);

            canHandle.Should().Be(mustApply);
        }

        public static IEnumerable<object[]> ApplyTo_TestData =>
            new[]
            {
                new object[] { new ArgumentException(), true },
                new object[] { new ArgumentOutOfRangeException(), true },
                new object[] { new InvalidCastException(), true },
                new object[] { new FormatException(), false }
            };

        [Theory, MemberData(nameof(Exclude_TestData))]
        public void Exclude_Exception_CanHandleReturnsExpectedResult(Exception exception, bool mustApply)
        {
            var policy = (TestErrorPolicy) new TestErrorPolicy()
                .Exclude<ArgumentException>()
                .Exclude<InvalidCastException>();

            var canHandle = policy.CanHandle(
                new InboundEnvelope(
                    new byte[1],
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "99") },
                    null, TestConsumerEndpoint.GetDefault(), TestConsumerEndpoint.GetDefault().Name),
                exception);

            canHandle.Should().Be(mustApply);
        }

        public static IEnumerable<object[]> Exclude_TestData =>
            new[]
            {
                new object[] { new ArgumentException(), false },
                new object[] { new ArgumentOutOfRangeException(), false },
                new object[] { new InvalidCastException(), false },
                new object[] { new FormatException(), true }
            };

        [Theory, MemberData(nameof(ApplyToAndExclude_TestData))]
        public void ApplyToAndExclude_Exception_CanHandleReturnsExpectedResult(Exception exception, bool mustApply)
        {
            var policy = (TestErrorPolicy) new TestErrorPolicy()
                .ApplyTo<ArgumentException>()
                .Exclude<ArgumentOutOfRangeException>()
                .ApplyTo<FormatException>();

            var canHandle = policy.CanHandle(
                new InboundEnvelope(
                    new byte[1],
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "99") },
                    null, TestConsumerEndpoint.GetDefault(), TestConsumerEndpoint.GetDefault().Name),
                exception);

            canHandle.Should().Be(mustApply);
        }

        public static IEnumerable<object[]> ApplyToAndExclude_TestData =>
            new[]
            {
                new object[] { new ArgumentException(), true },
                new object[] { new ArgumentNullException(), true },
                new object[] { new ArgumentOutOfRangeException(), false },
                new object[] { new InvalidCastException(), false },
                new object[] { new FormatException(), true }
            };

        [Theory, MemberData(nameof(ApplyWhen_TestData))]
        public void ApplyWhen_Exception_CanHandleReturnsExpectedResult(
            IInboundEnvelope envelope,
            Exception exception,
            bool mustApply)
        {
            var policy = (TestErrorPolicy) new TestErrorPolicy()
                .ApplyWhen((msg, ex) =>
                    msg.Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts) <= 5 && ex.Message != "no");

            var canHandle = policy.CanHandle(envelope, exception);

            canHandle.Should().Be(mustApply);
        }

        public static IEnumerable<object[]> ApplyWhen_TestData =>
            new[]
            {
                new object[]
                {
                    new InboundEnvelope(
                        new byte[1],
                        new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3") },
                        null, TestConsumerEndpoint.GetDefault(), TestConsumerEndpoint.GetDefault().Name),
                    new ArgumentException(),
                    true
                },
                new object[]
                {
                    new InboundEnvelope(
                        new byte[1],
                        new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "6") },
                        null, TestConsumerEndpoint.GetDefault(), TestConsumerEndpoint.GetDefault().Name),
                    new ArgumentException(),
                    false
                },
                new object[]
                {
                    new InboundEnvelope(
                        new byte[1],
                        new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3") },
                        null, TestConsumerEndpoint.GetDefault(), TestConsumerEndpoint.GetDefault().Name),
                    new ArgumentException("no"),
                    false
                }
            };

        [Fact]
        public async Task Publish_Exception_MessagePublished()
        {
            var publisher = Substitute.For<IPublisher>();
            var serviceProvider = new ServiceCollection().AddScoped(_ => publisher)
                .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            var policy =
                (TestErrorPolicy) new TestErrorPolicy(serviceProvider).Publish(msg => new TestEventTwo
                    { Content = "aaa" });
            var message = new InboundEnvelope(
                new byte[1],
                new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3") },
                null, TestConsumerEndpoint.GetDefault(), TestConsumerEndpoint.GetDefault().Name);


            await policy.HandleError(new[] { message }, new ArgumentNullException());

            publisher.Received().PublishAsync(Arg.Any<TestEventTwo>());
        }

        // TODO: Test with multiple messages (batch)
    }
}