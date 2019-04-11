// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;

namespace Silverback.Integration.Kafka.ConfigClassGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var xmlDocumentationPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @".nuget\packages\confluent.kafka\1.0.0-rc3\lib\netstandard1.3\Confluent.Kafka.xml");

            var consumerConfig = new ProxyClassGenerator(typeof(Confluent.Kafka.ConsumerConfig), "ConfluentConsumerConfigProxy", xmlDocumentationPath).Generate();
            Console.Write(consumerConfig);

            Console.WriteLine();
            Console.WriteLine();

            var producerConfig = new ProxyClassGenerator(typeof(Confluent.Kafka.ProducerConfig), "ConfluentProducerConfigProxy", xmlDocumentationPath).Generate();
            Console.Write(producerConfig);
        }
    }
}
