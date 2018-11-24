using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// A chain of error policies to be applied one after another.
    /// </summary>
    public class ErrorPolicyChain : ErrorPolicyBase
    {
        private readonly ILogger<ErrorPolicyChain> _logger;
        private readonly ErrorPolicyBase[] _policies;

        public ErrorPolicyChain(ILogger<ErrorPolicyChain> logger, params ErrorPolicyBase[] policies)
            : this (policies.AsEnumerable(), logger)
        {
        }

        public ErrorPolicyChain(IEnumerable<ErrorPolicyBase> policies, ILogger<ErrorPolicyChain> logger)
            : base(logger)
        {
            _logger = logger;

            if (policies == null) throw new ArgumentNullException(nameof(policies));
            _policies = policies.ToArray();

            if (_policies.Any(p => p == null)) throw new ArgumentNullException(nameof(policies), "One or more policies in the chain have a null value.");
        }


        protected override void ApplyPolicy(IMessage message, Action<IMessage> messageHandler, Exception exception)
        {
            for (var i = 0; i < _policies.Length; i++)
            {
                _logger.LogTrace($"Applying chained policy {i+1} of {_policies.Length} ({_policies[i]}) to handle failed message {message.GetTraceString()}'.");

                try
                {
                    if (_policies[i].HandleException(message, messageHandler, exception))
                        break;
                }
                catch (Exception ex)
                {
                    if (i == _policies.GetUpperBound(0))
                        throw;

                    exception = ex; // TODO: Correct to overwrite the exception?
                }
            }
        }
    }
}