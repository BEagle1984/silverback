// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Configuration
{
    internal static class ClientConfigCloneExtensions
    {
        public static ClientConfig Clone(this ClientConfig source)
        {
            var cloneDictionary = new Dictionary<string, string>();

            foreach (var keyValuePair in source)
            {
                cloneDictionary.Add(keyValuePair.Key, keyValuePair.Value);
            }

            return new ClientConfig(cloneDictionary);
        }
    }
}
