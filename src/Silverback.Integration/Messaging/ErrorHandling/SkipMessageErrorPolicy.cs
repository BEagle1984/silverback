// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    ///     This policy simply skips the message that failed to be processed.
    /// </summary>
    public class SkipMessageErrorPolicy : ErrorPolicyBase
    {
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;

        public SkipMessageErrorPolicy(
            IServiceProvider serviceProvider,
            ILogger<SkipMessageErrorPolicy> logger,
            MessageLogger messageLogger)
            : base(serviceProvider, logger, messageLogger)
        {
            _logger = logger;
            _messageLogger = messageLogger;
        }

        protected override ErrorAction ApplyPolicy(IReadOnlyCollection<IInboundMessage> messages, Exception exception)
        {
            _messageLogger.LogWarning(_logger, exception, "The message(s) will be skipped.", messages);

            return ErrorAction.Skip;
        }
    }
}