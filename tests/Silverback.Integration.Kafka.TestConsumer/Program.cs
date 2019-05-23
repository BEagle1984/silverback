// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog.Extensions.Logging;
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

        private static void Main()
        {
            Console.Clear();

            PrintHeader();
            Setup();

            Connect();
            Console.CancelKeyPress += (_, e) => { Disconnect(); };

            while (true)
                Console.ReadLine();
        }

        private static void Connect()
        {
            var d = new DiagnosticListenerObserver(new DiagnosticObserver());
            DiagnosticListener.AllListeners.Subscribe(d);

            var listener = new DiagnosticListener("SilverbackConsumerDiagnosticListener");

            var messageKeyProvider = new MessageKeyProvider(new[] { new DefaultPropertiesMessageKeyProvider() });
            _broker = new KafkaBroker(messageKeyProvider, GetLoggerFactory(), new MessageLogger(messageKeyProvider), listener);

            var serializer = new JsonMessageSerializer<TestMessage>();
            serializer.Settings.TypeNameHandling = TypeNameHandling.None;

            _consumer = _broker.GetConsumer(new KafkaConsumerEndpoint("Topic1")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://kafka:9092",
                    GroupId = "silverback-consumer",
                    AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest
                },
                Serializer = serializer
            });

            _consumer.Received += OnMessageReceived;

            _broker.Connect();
        }

        private static void Setup()
        {
            
        }

        private static void Disconnect()
        {
            _broker.Disconnect();
        }

        private static void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            var testMessage = args.Message as TestMessage;
            var testMessageHeaders = args.Headers;

            if (testMessage == null)
            {
                Console.WriteLine("Received a weird message!");
                return;
            }

            Console.WriteLine($"[{testMessage.Id}] {testMessage.Text} (CorrelationId: {Activity.Current.Id}; Bag: {string.Join(' ', Activity.Current.Baggage.Select(b => b.Key + ":" + b.Value))})");

            var text = testMessage.Text.ToLower().Trim();
            if (text == "bad")
            {
                Console.WriteLine("--> Bad message, throwing exception!");
                throw new Exception("Bad!");
            }
            else if (text.StartsWith("delay"))
            {
                if (int.TryParse(text.Substring(5), out int delay) && delay > 0)
                {
                    Console.WriteLine($"--> Delaying execution of {delay} seconds!");
                    Thread.Sleep(delay * 1000);
                }
            }

            _consumer.Acknowledge(args.Offset);
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
            var loggerFactory = new LoggerFactory()
                .AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            NLog.LogManager.LoadConfiguration("nlog.config");

            return loggerFactory;
        }
    }
}
