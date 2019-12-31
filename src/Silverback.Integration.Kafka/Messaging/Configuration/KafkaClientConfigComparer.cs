// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Silverback.Messaging.Configuration
{
    internal class KafkaClientConfigComparer : IEqualityComparer<Confluent.Kafka.ClientConfig>
    {
        public bool Equals(Confluent.Kafka.ClientConfig x, Confluent.Kafka.ClientConfig y)
        {
            return Compare(x, y);
        }

        public int GetHashCode(Confluent.Kafka.ClientConfig obj)
        {
            var hashCodeReferer = $"{obj.Count()}-{obj.BootstrapServers}";

            unchecked
            {
                return obj.Count() + hashCodeReferer.GetHashCode();
            }
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public static bool Compare(Confluent.Kafka.ClientConfig x, Confluent.Kafka.ClientConfig y)
        {
            if (x == null || y == null) return false;
            if (x.Count() != y.Count()) return false;

            var groupedX = x.GroupBy(kvp => kvp.Key);
            var groupedY = y.GroupBy(kvp => kvp.Key);

            if (groupedX.Count() != groupedY.Count()) return false;

            foreach (var groupX in groupedX)
            {
                var groupY = groupedY.FirstOrDefault(g => g.Key == groupX.Key);

                if (groupY == null || groupX.Count() != groupY.Count())
                    return false;

                foreach (var pairX in groupX)
                {
                    var pairY = groupY.FirstOrDefault(g => g.Key == pairX.Key);

                    if (pairX.Value != pairY.Value)
                        return false;
                }
            }

            return true;
        }
    }
}