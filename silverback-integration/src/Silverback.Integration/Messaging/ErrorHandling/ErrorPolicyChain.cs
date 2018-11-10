using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// A chain of error policies to be applied one after another.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.ErrorHandling.ErrorPolicyBase" />
    public class ErrorPolicyChain : ErrorPolicyBase
    {
        private readonly ErrorPolicyBase[] _policies;
        private ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorPolicyChain"/> class.
        /// </summary>
        /// <param name="policies">The policies to be applied one after the other.</param>
        public ErrorPolicyChain(params ErrorPolicyBase[] policies)
            : this (policies.AsEnumerable())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorPolicyChain"/> class.
        /// </summary>
        /// <param name="policies">The policies to be applied one after the other.</param>
        public ErrorPolicyChain(IEnumerable<ErrorPolicyBase> policies)
        {
            if (policies == null) throw new ArgumentNullException(nameof(policies));
            _policies = policies.ToArray();

            if (_policies.Any(p => p == null)) throw new ArgumentNullException(nameof(policies), "One or more policies in the chain have a null value.");
        }

        /// <summary>
        /// Initializes the policy, binding to the specified bus.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public override void Init(IBus bus)
        {
            _logger = bus.GetLoggerFactory().CreateLogger<ErrorPolicyChain>();
            base.Init(bus);

            _policies?.ForEach(p => p.Init(bus));
        }

        /// <summary>
        /// Applies the error handling policies chain.
        /// </summary>
        /// <param name="envelope">The envelope containing the failed message.</param>
        /// <param name="handler">The method that was used to handle the message.</param>
        /// <param name="exception">The exception that occurred.</param>
        protected override void ApplyPolicyImpl(IEnvelope envelope, Action<IEnvelope> handler, Exception exception)
        {
            for (var i = 0; i < _policies.Length; i++)
            {
                _logger.LogInformation($"Applying chained policy {i+1} of {_policies.Length} ({_policies[i]}) to handle failed message '{envelope.Message.Id}'.");

                try
                {
                    if (_policies[i].ApplyPolicy(envelope, handler, exception))
                        break;
                }
                catch (Exception ex)
                {
                    if (i == _policies.GetUpperBound(0))
                        throw;

                    _logger.LogWarning(ex, $"The error policy has been applied but the message " +
                                           $"'{envelope.Message.Id}' still couldn't be successfully " +
                                           $"processed. Will continue applying the next policy. ");

                    exception = ex; // TODO: Correct to overwrite the exception?
                }
            }
        }
    }
}