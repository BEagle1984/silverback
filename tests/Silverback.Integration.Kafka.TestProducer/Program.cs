// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Integration.Kafka.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;

namespace Silverback.Integration.Kafka.TestProducer
{
    internal static class Program
    {
        private static IBroker _broker;
        private static IProducer _producer;
        private static Activity _activity;

        private static void Main()
        {
            Console.Clear();

            ConfigureServices();
            StartActivity();
            Connect();
            PrintUsage();
            HandleInput();

            _activity.Stop();
        }

        private static void StartActivity()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            _activity = new Activity("Main");
            _activity.AddBaggage("MyItem1", "someValue1");
            _activity.AddBaggage("MyItem2", "someValue2");
            _activity.Start();
        }

        private static void ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddLogging(l => l.SetMinimumLevel(LogLevel.Trace))
                .AddSilverback().WithConnectionToKafka();

            _broker = services.BuildServiceProvider().GetRequiredService<IBroker>();
        }

        private static void Connect()
        {
            _producer = _broker.GetProducer(new KafkaProducerEndpoint("Topic1")
            {
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092"
                }
            });

            _broker.Connect();

            Console.CancelKeyPress += (_, e) => { _broker.Disconnect(); };
        }

        private static void HandleInput()
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

                if (text != null)
                    Produce(text);
            }
        }

        private static void Produce(string text) =>
            _producer.Produce(new TestMessage
            {
                Id = Guid.NewGuid(),
                Text = text
            });

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
            Console.WriteLine("");
            Console.WriteLine("To post a message:");
            Console.WriteLine("> message<Enter>");
            Console.WriteLine("Press Ctrl-C to quit.");
            Console.WriteLine("");
        }
    }
}