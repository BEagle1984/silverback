// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Tools.Generators.KafkaConfigProxies;

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
