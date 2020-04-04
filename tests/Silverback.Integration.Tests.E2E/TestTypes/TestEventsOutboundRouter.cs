// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes
{
    public class TestPrioritizedOutboundRouter : OutboundRouter<TestPrioritizedCommand>
    {
        private static readonly IProducerEndpoint HighPriorityEndpoint =
            new KafkaProducerEndpoint("test-e2e-high");
        private static readonly IProducerEndpoint NormalPriorityEndpoint =
            new KafkaProducerEndpoint("test-e2e-normal");
        private static readonly IProducerEndpoint LowPriorityEndpoint =
            new KafkaProducerEndpoint("test-e2e-low");
        private static readonly IProducerEndpoint AllMessagesEndpoint =
            new KafkaProducerEndpoint("test-e2e-all");

        public override IEnumerable<IProducerEndpoint> Endpoints
        {
            get
            {
                yield return AllMessagesEndpoint;
                yield return LowPriorityEndpoint;
                yield return NormalPriorityEndpoint;
                yield return HighPriorityEndpoint;
            }
        }

        public override IEnumerable<IProducerEndpoint> GetDestinationEndpoints(
            TestPrioritizedCommand message,
            MessageHeaderCollection headers)
        {
            yield return AllMessagesEndpoint;
            
            switch (message.Priority)
            {
                case TestPrioritizedCommand.PriorityEnum.Low:
                    yield return LowPriorityEndpoint;
                    break;
                case TestPrioritizedCommand.PriorityEnum.High:
                    yield return HighPriorityEndpoint;
                    break;
                default:
                    yield return NormalPriorityEndpoint;
                    break;
            }
        }
    }
}