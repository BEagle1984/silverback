// Copyright (c) 2020 Sergio Aquilini
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

        /// <summary>
        ///     Creates a chain of multiple policies to be applied one after the other to handle the processing error.
        /// </summary>
        /// <param name="policies">The policies to be sequentially applied.</param>
        /// <returns></returns>
        public ErrorPolicyChain Chain(params ErrorPolicyBase[] policies) =>
            new ErrorPolicyChain(
                _serviceProvider,
                _loggerFactory.CreateLogger<ErrorPolicyChain>(),
                _serviceProvider.GetRequiredService<MessageLogger>(),
                policies);

        /// <summary>
        ///     Creates a retry policy to simply try again the message processing in case of processing errors.
        /// </summary>
        /// <param name="initialDelay">The optional delay between each retry.</param>
        /// <param name="delayIncrement">The optional increment to be added to the initial delay at each retry.</param>
        /// <returns></returns>
        public RetryErrorPolicy Retry(TimeSpan? initialDelay = null, TimeSpan? delayIncrement = null) =>
            new RetryErrorPolicy(
                _serviceProvider,
                _loggerFactory.CreateLogger<RetryErrorPolicy>(),
                _serviceProvider.GetRequiredService<MessageLogger>(),
                initialDelay, delayIncrement);

        /// <summary>
        ///     Creates a skip policy to discard the message whose processing failed.
        /// </summary>
        /// <returns></returns>
        public SkipMessageErrorPolicy Skip() =>
            new SkipMessageErrorPolicy(
                _serviceProvider,
                _loggerFactory.CreateLogger<SkipMessageErrorPolicy>(),
                _serviceProvider.GetRequiredService<MessageLogger>());

        /// <summary>
        ///     Creates a move policy to forward the message to another endpoint in case of processing errors.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public MoveMessageErrorPolicy Move(IProducerEndpoint endpoint) =>
            new MoveMessageErrorPolicy(
                _serviceProvider.GetRequiredService<IBrokerCollection>(), endpoint,
                _serviceProvider,
                _loggerFactory.CreateLogger<MoveMessageErrorPolicy>(),
                _serviceProvider.GetRequiredService<MessageLogger>());
    }
}