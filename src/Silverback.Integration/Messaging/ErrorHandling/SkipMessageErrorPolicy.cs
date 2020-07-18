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
        private readonly ISilverbackLogger _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SkipMessageErrorPolicy" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public SkipMessageErrorPolicy(
            IServiceProvider serviceProvider,
            ISilverbackLogger<SkipMessageErrorPolicy> logger)
            : base(serviceProvider, logger) =>
            _logger = logger;

        /// <inheritdoc cref="ErrorPolicyBase.ApplyPolicy" />
        protected override Task<ErrorAction> ApplyPolicy(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            Exception exception)
        {
            _logger.LogWithMessageInfo(
                LogLevel.Error,
                EventIds.SkipMessagePolicyMessageSkipped,
                exception,
                "The message(s) will be skipped.",
                envelopes);

            return Task.FromResult(ErrorAction.Skip);
        }
    }
}
