// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Tools.Generators.MqttConfigProxies;

public class State
{
    public Queue<Type> GeneratorQueue { get; } = new();

    public HashSet<Type> DiscoveredTypes { get; } = new();

    public void AddType<T>() => AddType(typeof(T));

    public void AddType(Type type)
    {
        if (DiscoveredTypes.Contains(type) || !TypesMapper.MustGenerate(type))
            return;

        DiscoveredTypes.Add(type);
        GeneratorQueue.Enqueue(type);
    }
}
