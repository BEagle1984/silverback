// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Creates an <see cref="InboundProcessorConsumerBehavior" /> per each consumer. This is done for
    ///     convenience, to easily handle the message batch and the commit / rollback callbacks.
    /// </summary>
    public class InboundProcessorConsumerBehaviorFactory : IConsumerBehaviorFactory
    {
        private readonly IErrorPolicyHelper _errorPolicyHelper;

        /// <summary>
        ///     Initializes a new instance of the <see cref="InboundProcessorConsumerBehaviorFactory" /> class.
        /// </summary>
        /// <param name="errorPolicyHelper">
        ///     The <see cref="IErrorPolicyHelper" /> to be used to apply the defined error policies.
        /// </param>
        public InboundProcessorConsumerBehaviorFactory(IErrorPolicyHelper errorPolicyHelper)
        {
            _errorPolicyHelper = errorPolicyHelper;
        }

        /// <inheritdoc cref="IBrokerBehaviorFactory{TBehavior}.Create" />
        public IConsumerBehavior Create() => new InboundProcessorConsumerBehavior(_errorPolicyHelper);

        // TODO: Check if still needed
    }
}
