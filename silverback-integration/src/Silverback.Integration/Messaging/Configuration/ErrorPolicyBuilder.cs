// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    public class ErrorPolicyBuilder // TODO: Test
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceProvider _serviceProvider;

        public ErrorPolicyBuilder(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
        }

        public ErrorPolicyChain Chain(params ErrorPolicyBase[] policies) =>
            new ErrorPolicyChain(_loggerFactory.CreateLogger<ErrorPolicyChain>(), policies);

        public RetryErrorPolicy Retry(TimeSpan? initialDelay = null,
            TimeSpan? delayIncreament = null) => new RetryErrorPolicy(_loggerFactory.CreateLogger<RetryErrorPolicy>(), initialDelay, delayIncreament);

        public SkipMessageErrorPolicy Skip() => new SkipMessageErrorPolicy(_loggerFactory.CreateLogger<SkipMessageErrorPolicy>());

        public MoveMessageErrorPolicy Move(IEndpoint endpoint) => new MoveMessageErrorPolicy(_serviceProvider.GetRequiredService<IBroker>(), endpoint, _loggerFactory.CreateLogger<MoveMessageErrorPolicy>());
    }
}
