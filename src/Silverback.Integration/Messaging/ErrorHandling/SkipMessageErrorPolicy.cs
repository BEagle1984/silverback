// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// This policy simply skips the message that failed to be processed.
    /// </summary>
    public class SkipMessageErrorPolicy : ErrorPolicyBase
    {
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;
        
        public SkipMessageErrorPolicy(IPublisher publisher, ILogger<SkipMessageErrorPolicy> logger, MessageLogger messageLogger)
            : base(publisher, logger, messageLogger)
        {
            _logger = logger;
            _messageLogger = messageLogger;
        }

        protected override ErrorAction ApplyPolicy(FailedMessage failedMessage, Exception exception)
        {
            _messageLogger.LogWarning(_logger, exception, "The message will be skipped.", failedMessage);

            return ErrorAction.Skip;
        }
    }
}