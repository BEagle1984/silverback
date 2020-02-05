// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    public class KafkaGroupIdFilterAttribute : MessageFilterAttribute
    {
        private readonly string[] _groupIdList;

        public KafkaGroupIdFilterAttribute(params string[] groupId)
        {
            _groupIdList = groupId;
        }

        public override bool MustProcess(object message) =>
            message is IInboundEnvelope inboundEnvelope &&
            inboundEnvelope.Endpoint is KafkaConsumerEndpoint endpoint &&
            _groupIdList.Any(groupId => groupId == endpoint.Configuration.GroupId);
    }
}