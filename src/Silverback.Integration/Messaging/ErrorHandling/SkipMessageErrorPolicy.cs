// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    ///     This policy simply skips the message that failed to be processed.
    /// </summary>
    public class SkipMessageErrorPolicy : ErrorPolicyBase
    {
        private readonly ILogger _logger;

        private LogLevel _logLevel = LogLevel.Error;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SkipMessageErrorPolicy" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ILogger" />.
        /// </param>
        public SkipMessageErrorPolicy(
            IServiceProvider serviceProvider,
            ILogger<SkipMessageErrorPolicy> logger)
            : base(serviceProvider, logger) =>
            _logger = logger;

        /// <summary>
        ///     Specifies the log level to be used when writing the "message skipped" log entry.
        /// </summary>
        /// <param name="logLevel">
        ///     The <see cref="LogLevel" /> to be used.
        /// </param>
        /// <returns>
        ///     The <see cref="SkipMessageErrorPolicy" /> so that additional calls can be chained.
        /// </returns>
        public SkipMessageErrorPolicy LogWithLevel(LogLevel logLevel)
        {
            _logLevel = logLevel;
            return this;
        }

        /// <inheritdoc />
        protected override Task<ErrorAction> ApplyPolicy(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            Exception exception)
        {
            _logger.Log(
                _logLevel,
                EventIds.SkipMessagePolicyMessageSkipped,
                exception,
                "The message(s) will be skipped.",
                envelopes);

            return Task.FromResult(ErrorAction.Skip);
        }
    }
}
