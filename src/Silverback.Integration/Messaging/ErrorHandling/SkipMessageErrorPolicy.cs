// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        private LogLevel _logLevel = LogLevel.Error;

        public SkipMessageErrorPolicy(
            IServiceProvider serviceProvider,
            ILogger<SkipMessageErrorPolicy> logger,
            MessageLogger messageLogger)
            : base(serviceProvider, logger, messageLogger)
        {
            _logger = logger;
            _messageLogger = messageLogger;
        }

        /// <summary>
        ///     Specifies the log level to be used when writing the "message skipped" log entry.
        /// </summary>
        /// <param name="logLevel">The <see cref="LogLevel"/> to be used.</param>
        public SkipMessageErrorPolicy LogWithLevel(LogLevel logLevel)
        {
            _logLevel = logLevel;
            return this;
        }

        protected override Task<ErrorAction> ApplyPolicy(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            Exception exception)
        {
            _messageLogger.Log(_logger, _logLevel, exception, "The message(s) will be skipped.", envelopes);

            return Task.FromResult(ErrorAction.Skip);
        }
    }
}
