// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    internal class ErrorPolicyBuilder : IErrorPolicyBuilder
    {
        private readonly ILoggerFactory _loggerFactory;

        private readonly IServiceProvider _serviceProvider;

        public ErrorPolicyBuilder(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
        }

        public ErrorPolicyChain Chain(params ErrorPolicyBase[] policies) =>
            new ErrorPolicyChain(
                _serviceProvider,
                _loggerFactory.CreateLogger<ErrorPolicyChain>(),
                policies);

        public RetryErrorPolicy Retry(TimeSpan? initialDelay = null, TimeSpan? delayIncrement = null) =>
            new RetryErrorPolicy(
                _serviceProvider,
                _loggerFactory.CreateLogger<RetryErrorPolicy>(),
                initialDelay,
                delayIncrement);

        public SkipMessageErrorPolicy Skip() =>
            new SkipMessageErrorPolicy(
                _serviceProvider,
                _loggerFactory.CreateLogger<SkipMessageErrorPolicy>());

        public MoveMessageErrorPolicy Move(IProducerEndpoint endpoint) =>
            new MoveMessageErrorPolicy(
                _serviceProvider.GetRequiredService<IBrokerCollection>(),
                endpoint,
                _serviceProvider,
                _loggerFactory.CreateLogger<MoveMessageErrorPolicy>());
    }
}
