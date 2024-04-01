// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Consuming.ErrorHandling;

namespace Silverback.Messaging.Configuration;

internal class ErrorPolicyChainBuilder : IErrorPolicyChainBuilder
{
    private readonly List<ErrorPolicyBase> _errorPolicies = [];

    public IErrorPolicyChainBuilder ThenStop(Action<StopConsumerErrorPolicyBuilder>? policyBuilderAction = null)
    {
        StopConsumerErrorPolicyBuilder builder = new();
        policyBuilderAction?.Invoke(builder);
        _errorPolicies.Add(builder.Build());
        return this;
    }

    public IErrorPolicyChainBuilder ThenSkip(Action<SkipMessageErrorPolicyBuilder>? policyBuilderAction = null)
    {
        SkipMessageErrorPolicyBuilder builder = new();
        policyBuilderAction?.Invoke(builder);
        _errorPolicies.Add(builder.Build());
        return this;
    }

    public IErrorPolicyChainBuilder ThenRetry(Action<RetryErrorPolicyBuilder>? policyBuilderAction = null)
    {
        RetryErrorPolicyBuilder builder = new();
        policyBuilderAction?.Invoke(builder);
        _errorPolicies.Add(builder.Build());
        return this;
    }

    public IErrorPolicyChainBuilder ThenRetry(int retriesCount, Action<RetryErrorPolicyBuilder>? policyBuilderAction = null)
    {
        RetryErrorPolicyBuilder builder = new();
        builder.WithMaxRetries(retriesCount);
        policyBuilderAction?.Invoke(builder);
        _errorPolicies.Add(builder.Build());
        return this;
    }

    public IErrorPolicyChainBuilder ThenMoveTo(string endpointName, Action<MoveMessageErrorPolicyBuilder>? policyBuilderAction = null)
    {
        MoveMessageErrorPolicyBuilder builder = new(endpointName);
        policyBuilderAction?.Invoke(builder);
        _errorPolicies.Add(builder.Build());
        return this;
    }

    public IErrorPolicy Build()
    {
        if (_errorPolicies.Count == 0)
            throw new SilverbackConfigurationException("At least 1 error policy is required.");

        if (_errorPolicies.Count == 1)
            return _errorPolicies[0];

        return new ErrorPolicyChain(_errorPolicies);
    }
}
