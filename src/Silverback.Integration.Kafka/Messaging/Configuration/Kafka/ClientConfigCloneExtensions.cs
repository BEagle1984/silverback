// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

internal static class ClientConfigCloneExtensions
{
    public static TClientConfig Clone<TClientConfig>(this TClientConfig source)
        where TClientConfig : ClientConfig =>
        CloneAs<TClientConfig>(source);

    public static TClientConfig CloneAs<TClientConfig>(this ClientConfig source)
        where TClientConfig : ClientConfig
    {
        Dictionary<string, string> cloneDictionary = new();

        foreach (KeyValuePair<string, string> keyValuePair in source)
        {
            cloneDictionary.Add(keyValuePair.Key, keyValuePair.Value);
        }

        ClientConfig clone;

        if (typeof(TClientConfig) == typeof(ProducerConfig))
            clone = new ProducerConfig(cloneDictionary);
        else if (typeof(TClientConfig) == typeof(ConsumerConfig))
            clone = new ConsumerConfig(cloneDictionary);
        else
            clone = new ClientConfig(cloneDictionary);

        return (TClientConfig)clone;
    }

}
