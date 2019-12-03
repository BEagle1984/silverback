// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Integration.Kafka.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Integration.Kafka.TestConsumer
{
    internal static class Program
    {
        private static IBroker _broker;
        private static IConsumer _consumer;

        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
        private static void Main()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            var activity = new Activity("Main");
            activity.Start();

            Console.Clear();
            
            PrintHeader();

            Connect();
            Console.CancelKeyPress += (_, e) =>
            {
                Disconnect();
                activity.Stop();
            };

            while (true)
                Console.ReadLine();
        }

        private static void Connect()
        {
            var messageKeyProvider = new MessageKeyProvider(new[] {new DefaultPropertiesMessageKeyProvider()});
            _broker = new KafkaBroker(messageKeyProvider, GetLoggerFactory(), new MessageLogger());

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
        }
        private static void Disconnect()
        {
            _broker.Disconnect();
        }

        private static async Task OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            var testMessage = (TestMessage)args.Endpoint.Serializer.Deserialize(args.Message, new MessageHeaderCollection(args.Headers));
            
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

            await _consumer.Acknowledge(args.Offset);
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

        private static ILoggerFactory GetLoggerFactory()
        {
            return LoggerFactory.Create(builder =>
                {
                    builder.AddFilter("*", LogLevel.Warning)
                        .AddFilter("Silverback.*", LogLevel.Trace)
                        .AddConsole();
                }
            );
        }
    }
}
