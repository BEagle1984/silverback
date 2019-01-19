// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;

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
            new ErrorPolicyChain(_loggerFactory.CreateLogger<ErrorPolicyChain>(),
                _serviceProvider.GetRequiredService<MessageLogger>(), policies);

        public RetryErrorPolicy Retry(TimeSpan? initialDelay = null, TimeSpan? delayIncrement = null) =>
            new RetryErrorPolicy(_loggerFactory.CreateLogger<RetryErrorPolicy>(),
                _serviceProvider.GetRequiredService<MessageLogger>(), initialDelay, delayIncrement);

        public SkipMessageErrorPolicy Skip() =>
            new SkipMessageErrorPolicy(
                _loggerFactory.CreateLogger<SkipMessageErrorPolicy>(),
                _serviceProvider.GetRequiredService<MessageLogger>());

        public MoveMessageErrorPolicy Move(IEndpoint endpoint) =>
            new MoveMessageErrorPolicy(
                _serviceProvider.GetRequiredService<IBroker>(), endpoint,
                _loggerFactory.CreateLogger<MoveMessageErrorPolicy>(),
                _serviceProvider.GetRequiredService<MessageLogger>());
    }
}
