// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Integration.Kafka.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Integration.Kafka.TestConsumer
{
    internal static class Program
    {
        private static Activity _activity;
        private static IBroker _broker;
        private static IConsumer _consumer;

        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
        private static void Main()
        {
            Console.Clear();

            StartActivity();
            PrintHeader();
            ConfigureServices();
            Connect();

            while (true)
                Console.ReadLine();
        }

        private static void StartActivity()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            _activity = new Activity("Main");
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
            _consumer = _broker.GetConsumer(new KafkaConsumerEndpoint("Topic1")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    GroupId = "silverback-consumer",
                    AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest
                },
                Serializer = new JsonMessageSerializer<TestMessage>()
            });

            _consumer.Received += OnMessageReceived;

            _broker.Connect();
            
            Console.CancelKeyPress += (_, e) =>
            {
                _broker.Disconnect();
                _activity.Stop();
            };
        }

        private static async Task OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            var testMessage = (TestMessage) args.Message.Endpoint.Serializer.Deserialize(
                args.Message.RawContent,
                new MessageHeaderCollection(args.Message.Headers));
            
            Console.WriteLine($"[{testMessage.Id}] [{Activity.Current.Id}] {testMessage.Text}");

            var text = testMessage.Text.ToLower().Trim();
            if (text == "bad")
            {
                Console.WriteLine("--> Bad message, throwing exception!");
                throw new Exception("Bad!");
            }

            if (text.StartsWith("delay"))
            {
                if (int.TryParse(text.Substring(5), out int delay) && delay > 0)
                {
                    Console.WriteLine($"--> Delaying execution of {delay} seconds!");
                    Thread.Sleep(delay * 1000);
                }
            }

            await _consumer.Acknowledge(args.Message.Offset);
        }

        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(@"  _  __      __ _          _____                                          ");
            Console.WriteLine(@" | |/ /     / _| |        / ____|                                         ");
            Console.WriteLine(@" | ' / __ _| |_| | ____ _| |     ___  _ __  ___ _   _ _ __ ___   ___ _ __ ");
            Console.WriteLine(@" |  < / _` |  _| |/ / _` | |    / _ \| '_ \/ __| | | | '_ ` _ \ / _ \ '__|");
            Console.WriteLine(@" | . \ (_| | | |   < (_| | |___| (_) | | | \__ \ |_| | | | | | |  __/ |   ");
            Console.WriteLine(@" |_|\_\__,_|_| |_|\_\__,_|\_____\___/|_| |_|___/\__,_|_| |_| |_|\___|_|   ");
            Console.ResetColor();
            Console.WriteLine("\nCtrl-C to quit.\n");
        }
    }
}
