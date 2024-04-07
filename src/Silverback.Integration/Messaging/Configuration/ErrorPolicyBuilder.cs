// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Consuming.ErrorHandling;

namespace Silverback.Messaging.Configuration;

internal class ErrorPolicyBuilder : IErrorPolicyBuilder
{
    private readonly ErrorPolicyChainBuilder _chainBuilder = new();

    public IErrorPolicyChainBuilder Stop(Action<StopConsumerErrorPolicyBuilder>? policyBuilderAction = null) =>
        _chainBuilder.ThenStop(policyBuilderAction);

    public IErrorPolicyChainBuilder Skip(Action<SkipMessageErrorPolicyBuilder>? policyBuilderAction = null) =>
        _chainBuilder.ThenSkip(policyBuilderAction);

    public IErrorPolicyChainBuilder Retry(Action<RetryErrorPolicyBuilder>? policyBuilderAction = null) =>
        _chainBuilder.ThenRetry(policyBuilderAction);

    public IErrorPolicyChainBuilder Retry(int retriesCount, Action<RetryErrorPolicyBuilder>? policyBuilderAction = null) =>
        _chainBuilder.ThenRetry(retriesCount, policyBuilderAction);

    public IErrorPolicyChainBuilder MoveTo(string endpointName, Action<MoveMessageErrorPolicyBuilder>? policyBuilderAction = null) =>
        _chainBuilder.ThenMoveTo(endpointName, policyBuilderAction);

    public IErrorPolicy Build() => _chainBuilder.Build();
}
