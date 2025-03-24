// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Silverback.Tools.Generators.KafkaConfigProxies;

if (args.Length == 0)
{
    Console.Write(new ParentProxyClassGenerator(typeof(ClientConfig)).Generate());
    Console.WriteLine();
    Console.Write(new ProxyClassGenerator(typeof(ConsumerConfig)).Generate());
    Console.WriteLine();
    Console.Write(new ProxyClassGenerator(typeof(ProducerConfig)).Generate());

    Console.WriteLine();

    Console.Write(new ParentBuilderGenerator(typeof(ClientConfig)).Generate());
    Console.WriteLine();
    Console.Write(new BuilderGenerator(typeof(ConsumerConfig)).Generate());
    Console.WriteLine();
    Console.Write(new BuilderGenerator(typeof(ProducerConfig)).Generate());
}
else if (args.Length == 1 && (args[0] == "--schemaregistry" || args[0] == "-sr"))
{
    Console.Write(new SchemaRegistryProxyClassGenerator(typeof(SchemaRegistryConfig)).Generate());

    Console.WriteLine();

    Console.Write(new SchemaRegistryBuilderGenerator(typeof(SchemaRegistryConfig)).Generate());
}
else
{
    Console.WriteLine("Invalid arguments.");
}
