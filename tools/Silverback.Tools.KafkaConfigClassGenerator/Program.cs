﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Reflection;
using Confluent.Kafka;

namespace Silverback.Tools.KafkaConfigClassGenerator;

internal static class Program
{
    private static void Main()
    {
        Assembly? assembly = Assembly.GetAssembly(typeof(ClientConfig));

        if (assembly == null)
            throw new InvalidOperationException("Couldn't load ClientConfig assembly.");

        string xmlDocumentationPath = Path.Combine(
            Path.GetDirectoryName(assembly.Location)!,
            "Confluent.Kafka.xml");

        Console.Write(
            new ProxyClassGenerator(
                    typeof(ClientConfig),
                    "ConfluentClientConfigProxy",
                    null,
                    null,
                    xmlDocumentationPath,
                    false)
                .Generate());

        Console.WriteLine();

        Console.Write(
            new ProxyClassGenerator(
                    typeof(ConsumerConfig),
                    "ConfluentConsumerConfigProxy",
                    "ConfluentClientConfigProxy",
                    "Confluent.Kafka.ConsumerConfig",
                    xmlDocumentationPath,
                    false)
                .Generate());

        Console.WriteLine();

        Console.Write(
            new ProxyClassGenerator(
                    typeof(ProducerConfig),
                    "ConfluentProducerConfigProxy",
                    "ConfluentClientConfigProxy",
                    "Confluent.Kafka.ProducerConfig",
                    xmlDocumentationPath,
                    false)
                .Generate());
    }
}
