// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.LargeMessages
{
    public class OffloadProducerHelper
    {
        private static readonly Dictionary<Type, bool> MessageTypesCache = new Dictionary<Type, bool>();

        private readonly IOffloadStoreWriter _storeWriter;
        private readonly MessageKeyProvider _messageKeyProvider;

        protected OffloadProducerHelper(IOffloadStoreWriter storeWriter, MessageKeyProvider messageKeyProvider)
        {
            _storeWriter = storeWriter;
            _messageKeyProvider = messageKeyProvider;
        }

        public object Offload(object message, IEndpoint endpoint)
        {
            var messageType = message.GetType();

            if (!MustOffload(messageType))
                return message;

            var toPublish = new Dictionary<string, object>();
            var toOffload = new Dictionary<string, object>();

            foreach (var prop in messageType.GetProperties())
            {
                if (prop.GetCustomAttribute<OffloadAttribute>() != null)
                    toOffload.Add(prop.Name, prop.GetValue(message));
                else
                    toPublish.Add(prop.Name, prop.GetValue(message));
            }

            _storeWriter.Store(_messageKeyProvider.GetKey(message), endpoint.Serializer.Serialize(toOffload));

            return toPublish;
        }

        private bool MustOffload(Type type)
        {
            if (!MessageTypesCache.ContainsKey(type))
            {
                lock (MessageTypesCache)
                {
                    MessageTypesCache.Add(
                        type,
                        type.GetProperties()
                            .Any(p => p.GetCustomAttribute<OffloadAttribute>(true) != null));
                }
            }

            return MessageTypesCache[type];
        }
    }
}
