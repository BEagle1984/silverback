// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Messages;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Broker;

namespace AsyncProducer
{
    internal static class Program
    {
        private static KafkaBroker _broker;
        private static IProducer _producer;

        private static void Main()
        {
            Console.Clear();

            Connect();
            Console.CancelKeyPress += (_, e) => { Disconnect(); };

            PrintUsage();
            HandleInput(_producer);
        }

        private static void Connect()
        {
            _broker = new KafkaBroker(GetLoggerFactory());
            _broker.Connect();

            _producer = _broker.GetProducer(new KafkaProducerEndpoint("Topic1")
            {
                Configuration = new Confluent.Kafka.ProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://kafka:9092",
                    ClientId = "ClientTest",
                    MessageSendMaxRetries = 0,
                    BatchNumMessages = 1,
                    SocketBlockingMaxMs = 1,
                    SocketNagleDisable = true,
                    QueueBufferingMaxMessages = 0,
                    Acks = 0
                }
            });
        }

        private static void Disconnect()
        {
            _broker.Disconnect();
            _broker.Dispose();
        }

        private static void HandleInput(IProducer producer)
        {
            while (true)
            {
                Console.Write("> ");

                string text;
                try
                {
                    text = Console.ReadLine();
                }
                catch (IOException)
                {
                    // IO exception is thrown when ConsoleCancelEventArgs.Cancel == true.
                    break;
                }

                if (text == null)
                {
                    continue;
                }

                Produce(producer, text);
            }
        }

        private static void Produce(IProducer producer, string text)
        {
            producer.ProduceAsync(new TestMessage
            {
                Id = Guid.NewGuid(),
                Text = text,
                Type = "TestMessage"
            }).Wait();
        }

        private static void PrintUsage()
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(@"  _  __      __ _           _____               _                     ");
            Console.WriteLine(@" | |/ /     / _| |         |  __ \             | |                    ");
            Console.WriteLine(@" | ' / __ _| |_| | ____ _  | |__) | __ ___   __| |_   _  ___ ___ _ __ ");
            Console.WriteLine(@" |  < / _` |  _| |/ / _` | |  ___/ '__/ _ \ / _` | | | |/ __/ _ \ '__|");
            Console.WriteLine(@" | . \ (_| | | |   < (_| | | |   | | | (_) | (_| | |_| | (_|  __/ |   ");
            Console.WriteLine(@" |_|\_\__,_|_| |_|\_\__,_| |_|   |_|  \___/ \__,_|\__,_|\___\___|_|   ");
            Console.ResetColor();
            Console.WriteLine("\nTo post a message:");
            Console.WriteLine("> message<Enter>");
            Console.WriteLine("Ctrl-C to quit.\n");
        }

        private static ILoggerFactory GetLoggerFactory()
        {
            var loggerFactory = new LoggerFactory()
                .AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            NLog.LogManager.LoadConfiguration("nlog.config");

            return loggerFactory;
        }
    }
}
