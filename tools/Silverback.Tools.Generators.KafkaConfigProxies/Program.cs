// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Tools.Generators.KafkaConfigProxies;

if (args.Length == 1 && (args[0] == "--schemaregistry" || args[0] == "-sr"))
{
    Type[] serdeConfigTypes =
    [
        typeof(AvroSerializerConfig),
        typeof(AvroDeserializerConfig),
        typeof(JsonSerializerConfig),
        typeof(JsonDeserializerConfig),
        typeof(ProtobufSerializerConfig),
        typeof(ProtobufDeserializerConfig)
    ];

    Console.Write(new SchemaRegistryProxyClassGenerator(typeof(SchemaRegistryConfig)).Generate());

    foreach (Type serdeConfigType in serdeConfigTypes)
    {
        Console.WriteLine();
        Console.Write(new SerdeProxyClassGenerator(serdeConfigType).Generate());
    }

    Console.WriteLine();

    Console.Write(new SchemaRegistryBuilderGenerator(typeof(SchemaRegistryConfig)).Generate());

    foreach (Type serdeConfigType in serdeConfigTypes)
    {
        Console.WriteLine();
        Console.Write(new SerdeBuilderGenerator(serdeConfigType).Generate());
    }
}
else
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
