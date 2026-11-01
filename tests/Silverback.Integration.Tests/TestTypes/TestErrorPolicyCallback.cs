// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes;

public sealed class TestErrorPolicyCallback
    : ISkipMessageErrorPolicyCallback,
        IRetryErrorPolicyCallback,
        IMoveMessageErrorPolicyCallback,
        IStopConsumerErrorPolicyCallback
{
    private readonly List<string> _invocations = [];

    private readonly List<IRawInboundEnvelope> _envelopes = [];

    private readonly List<Exception> _exceptions = [];

    public IReadOnlyList<string> Invocations => _invocations;

    public IReadOnlyList<IRawInboundEnvelope> Envelopes => _envelopes;

    public IReadOnlyList<Exception> Exceptions => _exceptions;

    public Task OnSkippingMessageAsync(IRawInboundEnvelope envelope, Exception exception) =>
        TrackInvocationAsync(nameof(OnSkippingMessageAsync), envelope, exception);

    public Task OnRetryingMessageAsync(IRawInboundEnvelope envelope, Exception exception) =>
        TrackInvocationAsync(nameof(OnRetryingMessageAsync), envelope, exception);

    public Task OnMovingMessageAsync(IRawInboundEnvelope envelope, Exception exception) =>
        TrackInvocationAsync(nameof(OnMovingMessageAsync), envelope, exception);

    public Task OnStoppingConsumerAsync(IRawInboundEnvelope envelope, Exception exception) =>
        TrackInvocationAsync(nameof(OnStoppingConsumerAsync), envelope, exception);

    private Task TrackInvocationAsync(string callbackName, IRawInboundEnvelope envelope, Exception exception)
    {
        _invocations.Add(callbackName);
        _envelopes.Add(envelope);
        _exceptions.Add(exception);
        return Task.CompletedTask;
    }
}
