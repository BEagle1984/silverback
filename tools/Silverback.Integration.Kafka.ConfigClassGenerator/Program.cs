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

            var consumerConfig = new ProxyClassGenerator(typeof(Confluent.Kafka.ConsumerConfig), "ConfluentConsumerConfigProxy", xmlDocumentationPath).Generate();
            Console.Write(consumerConfig);

            Console.WriteLine();
            Console.WriteLine();

            var producerConfig = new ProxyClassGenerator(typeof(Confluent.Kafka.ProducerConfig), "ConfluentProducerConfigProxy", xmlDocumentationPath).Generate();
            Console.Write(producerConfig);
        }
    }
}
