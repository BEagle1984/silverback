// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// This policy moves the failed messages to the configured endpoint.
    /// </summary>
    public class MoveMessageErrorPolicy : ErrorPolicyBase
    {
        private readonly IProducer _producer;
        private Func<FailedMessage, Exception, IMessage> _transformationFunction;

        public MoveMessageErrorPolicy(IBroker broker, IEndpoint endpoint, ILogger<MoveMessageErrorPolicy> logger) 
            : base(logger)
        {
            _producer = broker.GetProducer(endpoint);
        }

        public MoveMessageErrorPolicy Transform(Func<FailedMessage, Exception, IMessage> transformationFunction)
        {
            _transformationFunction = transformationFunction;
            return this;
        }

        public override ErrorAction HandleError(FailedMessage failedMessage, Exception exception)
        {
            _producer.Produce(_transformationFunction?.Invoke(failedMessage, exception) ?? failedMessage);

            return ErrorAction.SkipMessage;
        }
    }
}