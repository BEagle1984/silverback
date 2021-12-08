// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;

namespace Silverback.Tools.KafkaConfigClassGenerator;

internal static class Program
{
    private static void Main()
    {
        Console.Write(new ProxyClassGenerator(typeof(ClientConfig)).Generate());
        Console.WriteLine();
        Console.Write(new ProxyClassGenerator(typeof(ConsumerConfig)).Generate());
        Console.WriteLine();
        Console.Write(new ProxyClassGenerator(typeof(ProducerConfig)).Generate());

        Console.WriteLine();

        Console.Write(new BuilderGenerator(typeof(ClientConfig)).Generate());
        Console.WriteLine();
        Console.Write(new BuilderGenerator(typeof(ConsumerConfig)).Generate());
        Console.WriteLine();
        Console.Write(new BuilderGenerator(typeof(ProducerConfig)).Generate());
    }
}
