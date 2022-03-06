// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using MQTTnet;

namespace Silverback.Messaging.Broker.Mqtt.Mocks;

internal class SharedSubscriptionsManager
{
    private readonly Dictionary<string, int> _groups = new();

    private readonly Dictionary<string, Dictionary<object, int>> _counters = new();

    public void Add(string group)
    {
        lock (_groups)
        {
            if (_groups.ContainsKey(group))
            {
                _groups[group]++;
            }
            else
            {
                _groups[group] = 0;
                _counters[group] = new Dictionary<object, int>();
            }
        }
    }

    public bool IsFirstMatch(string group, MqttApplicationMessage message)
    {
        lock (_counters)
        {
            Dictionary<object, int> counter = _counters[group];

            if (counter.ContainsKey(message))
            {
                counter[message]++;
                if (counter[message] >= _groups[group])
                    counter.Remove(message);

                return false;
            }

            counter[message] = 1;
            return true;
        }
    }
}
