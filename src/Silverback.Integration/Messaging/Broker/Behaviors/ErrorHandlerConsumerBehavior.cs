// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.ErrorHandling;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Applies the error policies.
    /// </summary>
    public class ErrorHandlerConsumerBehavior : IConsumerBehavior
    {
        private readonly IErrorPolicyHelper _errorPolicyHelper;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorHandlerConsumerBehavior" /> class.
        /// </summary>
        /// <param name="errorPolicyHelper">
        ///     The <see cref="IErrorPolicyHelper" /> to be used to apply the defined error policies.
        /// </param>
        public ErrorHandlerConsumerBehavior(IErrorPolicyHelper errorPolicyHelper)
        {
            _errorPolicyHelper = Check.NotNull(errorPolicyHelper, nameof(errorPolicyHelper));
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.ErrorHandler;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            // TODO:
            // * Invoke policies, passing consumer to perform the necessary action (seek, move, etc.)
            // * Handle rebalance during seek / move (don't crash)
            // * Count failed attempts
            // * Can probably move the logic in here and get rid of the helper?

            await next(context).ConfigureAwait(false);
        }
    }
}
