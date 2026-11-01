// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Consuming.ErrorHandling;

internal sealed class ErrorPolicyCallbacksInvoker : CallbacksInvoker<IErrorPolicyCallback, ErrorPolicyCallbackInvocationException>
{
    // The application lifetime isn't needed, since the error policy callbacks are always invoked, even during the shutdown
    public ErrorPolicyCallbacksInvoker(
        IServiceScopeFactory serviceScopeFactory,
        ISilverbackLogger<ErrorPolicyCallbacksInvoker> logger)
        : base(serviceScopeFactory, null, logger)
    {
    }

    protected override ErrorPolicyCallbackInvocationException CreateInvocationException(string message, Exception innerException) =>
        new(message, innerException);
}
