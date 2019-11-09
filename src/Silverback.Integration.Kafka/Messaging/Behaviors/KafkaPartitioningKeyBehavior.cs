// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Behaviors
{
    public class KafkaPartitioningKeyBehavior : ISortedBehavior
    {
        private static readonly HashAlgorithm HashAlgorithm = MD5.Create();

        public KafkaPartitioningKeyBehavior()
        {
        }

        public int SortIndex { get; } = 200;

        public Task<IEnumerable<object>> Handle(IEnumerable<object> messages, MessagesHandler next)
        {
            messages.OfType<IOutboundMessage>().ForEach(SetPartitioningKey);
            return next(messages);
        }

        private void SetPartitioningKey(IOutboundMessage outboundMessage)
        {
            var key = KeyHelper.GetMessageKey(outboundMessage.Content);

            if (key == null)
                return;

            var keyHash = Convert.ToBase64String(HashAlgorithm.ComputeHash(key));

            outboundMessage.Headers.AddOrReplace(KafkaProducer.PartitioningKeyHeaderKey, keyHash);
        }
    }
}