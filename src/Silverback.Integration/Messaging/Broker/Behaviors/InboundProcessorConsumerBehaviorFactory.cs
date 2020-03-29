// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Broker.Behaviors
{
    public class InboundProcessorConsumerBehaviorFactory : IConsumerBehaviorFactory
    {
        private readonly ErrorPolicyHelper _errorPolicyHelper;

        public InboundProcessorConsumerBehaviorFactory(ErrorPolicyHelper errorPolicyHelper)
        {
            _errorPolicyHelper = errorPolicyHelper;
        }

        public IConsumerBehavior Create() => new InboundProcessorConsumerBehavior(_errorPolicyHelper);
    }
}