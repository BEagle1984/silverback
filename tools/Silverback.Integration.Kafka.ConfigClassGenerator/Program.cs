// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Reflection;

namespace Silverback.Integration.Kafka.ConfigClassGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var xmlDocumentationPath = Path.Combine(
                Path.GetDirectoryName(
                    Assembly.GetAssembly(typeof(Confluent.Kafka.ClientConfig)).Location),
                "Confluent.Kafka.xml");

            Console.Write(new ProxyClassGenerator(
                    typeof(Confluent.Kafka.ClientConfig),
                    "ConfluentClientConfigProxy",
                    null,
                    xmlDocumentationPath, false)
                .Generate());

            Console.WriteLine();

            Console.Write(new ProxyClassGenerator(
                    typeof(Confluent.Kafka.ConsumerConfig),
                    "ConfluentConsumerConfigProxy",
                    "ConfluentClientConfigProxy",
                    xmlDocumentationPath, false)
                .Generate());

            Console.WriteLine();

            Console.Write(new ProxyClassGenerator(
                    typeof(Confluent.Kafka.ProducerConfig),
                    "ConfluentProducerConfigProxy",
                    "ConfluentClientConfigProxy",
                    xmlDocumentationPath, false)
                .Generate());
        }
    }
}
