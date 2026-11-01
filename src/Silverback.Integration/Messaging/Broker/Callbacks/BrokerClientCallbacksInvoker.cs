// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Broker.Callbacks;

internal sealed class BrokerClientCallbacksInvoker
    : CallbacksInvoker<IBrokerClientCallback, BrokerClientCallbackInvocationException>, IBrokerClientCallbacksInvoker
{
    public BrokerClientCallbacksInvoker(
        IServiceScopeFactory serviceScopeFactory,
        IHostApplicationLifetime applicationLifetime,
        ISilverbackLogger<BrokerClientCallbacksInvoker> logger)
        : base(serviceScopeFactory, applicationLifetime, logger)
    {
    }

    protected override BrokerClientCallbackInvocationException CreateInvocationException(string message, Exception innerException) =>
        new(message, innerException);
}
