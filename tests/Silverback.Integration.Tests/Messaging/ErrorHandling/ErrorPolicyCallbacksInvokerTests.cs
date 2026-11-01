// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class ErrorPolicyCallbacksInvokerTests
{
    private readonly IRawInboundEnvelope _envelope = Substitute.For<IRawInboundEnvelope>();

    private readonly InvalidOperationException _exception = new("test");

    [Fact]
    public async Task InvokeAsync_ShouldInvokeMatchingCallbacks()
    {
        TestErrorPolicyCallback callback1 = new();
        TestErrorPolicyCallback callback2 = new();
        ServiceProvider serviceProvider = BuildServiceProvider(builder => builder
            .AddSingletonErrorPolicyCallback(callback1)
            .AddScopedErrorPolicyCallback(_ => callback2));
        ErrorPolicyCallbacksInvoker invoker = serviceProvider.GetRequiredService<ErrorPolicyCallbacksInvoker>();

        await invoker.InvokeAsync<ISkipMessageErrorPolicyCallback>(callback => callback.OnSkippingMessageAsync(_envelope, _exception));

        callback1.Invocations.ShouldBe([nameof(ISkipMessageErrorPolicyCallback.OnSkippingMessageAsync)]);
        callback1.Envelopes.ShouldBe([_envelope]);
        callback1.Exceptions.ShouldBe([_exception]);
        callback2.Invocations.ShouldBe([nameof(ISkipMessageErrorPolicyCallback.OnSkippingMessageAsync)]);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotInvokeNonMatchingCallbacks()
    {
        IRetryErrorPolicyCallback callback = Substitute.For<IRetryErrorPolicyCallback>();
        ServiceProvider serviceProvider = BuildServiceProvider(builder => builder.AddSingletonErrorPolicyCallback(callback));
        ErrorPolicyCallbacksInvoker invoker = serviceProvider.GetRequiredService<ErrorPolicyCallbacksInvoker>();

        await invoker.InvokeAsync<ISkipMessageErrorPolicyCallback>(skipCallback => skipCallback.OnSkippingMessageAsync(_envelope, _exception));

        await callback.DidNotReceive().OnRetryingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>());
    }

    [Fact]
    public async Task InvokeAsync_ShouldWrapCallbackException()
    {
        ISkipMessageErrorPolicyCallback callback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        callback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Failed.")));
        ServiceProvider serviceProvider = BuildServiceProvider(builder => builder.AddSingletonErrorPolicyCallback(callback));
        ErrorPolicyCallbacksInvoker invoker = serviceProvider.GetRequiredService<ErrorPolicyCallbacksInvoker>();

        Func<Task> act = () => invoker
            .InvokeAsync<ISkipMessageErrorPolicyCallback>(skipCallback => skipCallback.OnSkippingMessageAsync(_envelope, _exception))
            .AsTask();

        ErrorPolicyCallbackInvocationException exception = await act.ShouldThrowAsync<ErrorPolicyCallbackInvocationException>();
        exception.InnerException.ShouldBeOfType<InvalidOperationException>();
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
}
