// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class ErrorPoliciesHelperTests
{
    private readonly TestErrorPolicyCallback _callback = new();

    private readonly InvalidOperationException _exception = new("test");

    private readonly ServiceProvider _serviceProvider;

    public ErrorPoliciesHelperTests()
    {
        _serviceProvider = BuildServiceProvider(builder => builder.AddSingletonErrorPolicyCallback(_callback));
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldInvokeStopCallbacks_WhenStopPolicyIsApplied()
    {
        ConsumerPipelineContext context = CreateContext(new StopConsumerErrorPolicy());

        bool result = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        result.ShouldBeFalse();
        _callback.Invocations.ShouldBe([nameof(IStopConsumerErrorPolicyCallback.OnStoppingConsumerAsync)]);
        _callback.Envelopes.ShouldBe([context.Envelope]);
        _callback.Exceptions.ShouldBe([_exception]);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldInvokeStopCallbacks_WhenRetriesAreExhausted()
    {
        ConsumerPipelineContext context = CreateContext(
            new RetryErrorPolicy
            {
                MaxFailedAttempts = 2
            },
            failedAttempts: 3);

        bool result = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        result.ShouldBeFalse();
        _callback.Invocations.ShouldBe([nameof(IStopConsumerErrorPolicyCallback.OnStoppingConsumerAsync)]);
        _callback.Envelopes.ShouldBe([context.Envelope]);
        _callback.Exceptions.ShouldBe([_exception]);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldInvokeStopCallbacks_WhenExceptionIsExcluded()
    {
        ConsumerPipelineContext context = CreateContext(
            new SkipMessageErrorPolicy
            {
                ExcludedExceptions = [typeof(InvalidOperationException)]
            });

        bool result = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        result.ShouldBeFalse();
        _callback.Invocations.ShouldBe([nameof(IStopConsumerErrorPolicyCallback.OnStoppingConsumerAsync)]);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldInvokeStopCallbacks_WhenNoPolicyInChainCanHandle()
    {
        ConsumerPipelineContext context = CreateContext(
            new ErrorPolicyChain(
                new RetryErrorPolicy
                {
                    MaxFailedAttempts = 1
                },
                new SkipMessageErrorPolicy
                {
                    MaxFailedAttempts = 1
                }),
            failedAttempts: 3);

        bool result = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        result.ShouldBeFalse();
        _callback.Invocations.ShouldBe([nameof(IStopConsumerErrorPolicyCallback.OnStoppingConsumerAsync)]);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldNotInvokeStopCallbacks_WhenErrorIsHandled()
    {
        ConsumerPipelineContext context = CreateContext(new SkipMessageErrorPolicy());

        bool result = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        result.ShouldBeTrue();
        _callback.Invocations.ShouldBe([nameof(ISkipMessageErrorPolicyCallback.OnSkippingMessageAsync)]);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldInvokeStopCallbacksAndRethrow_WhenErrorHandlingThrows()
    {
        ISkipMessageErrorPolicyCallback skipCallback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        skipCallback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Skip callback failed.")));
        ServiceProvider serviceProvider = BuildServiceProvider(builder => builder
            .AddSingletonErrorPolicyCallback(skipCallback)
            .AddSingletonErrorPolicyCallback(_callback));
        ConsumerPipelineContext context = CreateContext(new SkipMessageErrorPolicy(), serviceProvider: serviceProvider);

        Func<Task> act = () => ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        ErrorPolicyCallbackInvocationException exception = await act.ShouldThrowAsync<ErrorPolicyCallbackInvocationException>();
        exception.InnerException?.Message.ShouldBe("Skip callback failed.");
        _callback.Invocations.ShouldBe([nameof(IStopConsumerErrorPolicyCallback.OnStoppingConsumerAsync)]);
        _callback.Envelopes.ShouldBe([context.Envelope]);
        _callback.Exceptions.ShouldBe([_exception]);
    }

    [Fact]
    public async Task ApplyErrorPoliciesAsync_ShouldRethrowErrorHandlingException_WhenStopCallbackThrows()
    {
        ISkipMessageErrorPolicyCallback skipCallback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        skipCallback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Skip callback failed.")));
        IStopConsumerErrorPolicyCallback stopCallback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        stopCallback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Stop callback failed.")));
        ServiceProvider serviceProvider = BuildServiceProvider(builder => builder
            .AddSingletonErrorPolicyCallback(skipCallback)
            .AddSingletonErrorPolicyCallback(stopCallback));
        ConsumerPipelineContext context = CreateContext(new SkipMessageErrorPolicy(), serviceProvider: serviceProvider);

        Func<Task> act = () => ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, _exception);

        ErrorPolicyCallbackInvocationException exception = await act.ShouldThrowAsync<ErrorPolicyCallbackInvocationException>();
        exception.InnerException?.Message.ShouldBe("Skip callback failed.");
        await stopCallback.Received(1).OnStoppingConsumerAsync(context.Envelope, _exception);
    }

    private static ServiceProvider BuildServiceProvider(Action<SilverbackBuilder> configureBuilder)
    {
        ServiceCollection services = [];

        SilverbackBuilder builder = services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker();

        configureBuilder.Invoke(builder);

        return services.BuildServiceProvider();
    }

    private ConsumerPipelineContext CreateContext(
        IErrorPolicy errorPolicy,
        int failedAttempts = 1,
        IServiceProvider? serviceProvider = null)
    {
        IConsumer consumer = Substitute.For<IConsumer>();
        InboundEnvelope envelope = new(
            new MemoryStream(),
            [],
            new TestConsumerEndpointConfiguration("test")
            {
                ErrorPolicy = errorPolicy
            }.GetDefaultEndpoint(),
            consumer,
            new TestOffset());
        consumer.IncrementFailedAttempts(envelope).Returns(failedAttempts);

        return ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider ?? _serviceProvider, consumer: consumer);
    }
}
