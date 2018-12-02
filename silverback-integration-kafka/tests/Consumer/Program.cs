using Messages;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using System;
using System.Collections.Generic;
using Silverback.Messaging.ErrorHandling;

namespace Consumer
{
    internal static class Program
    {
        private static KafkaBroker _broker;

        private static void Main()
        {
            Console.Clear();

            PrintHeader();

            Connect();
            Console.CancelKeyPress += (_, e) => { Disconnect(); };

            while (true)
                Console.ReadLine();
        }

        private static void Connect()
        {
            _broker = new KafkaBroker(GetLoggerFactory());

            var consumer = _broker.GetConsumer(new KafkaConsumerEndpoint("Topic1")
            {
                ConsumerThreads = 3,
                Configuration = new Confluent.Kafka.ConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://kafka:9092",
                    ClientId = "ClientTest",
                    GroupId = "advanced-silverback-consumer",
                    EnableAutoCommit = false,
                    AutoCommitIntervalMs = 5000,
                    StatisticsIntervalMs = 60000,
                    AutoOffsetReset = Confluent.Kafka.AutoOffsetResetType.Earliest
                }
            });

            consumer.Received += OnMessageReceived;
            consumer.Error += OnError;

            _broker.Connect();
        }
        private static void Disconnect()
        {
            _broker.Disconnect();
            _broker.Dispose();
        }

        private static void OnMessageReceived(object sender, IMessage message)
        {
            var testMessage = message as TestMessage;

            if (testMessage == null)
            {
                Console.WriteLine("Received a weird message!");
            }

            Console.WriteLine($"[{testMessage.Id}] {testMessage.Text}");

            if (testMessage.Text == "bad")
            {
                Console.WriteLine("--> Bad message, throwing exception!");
                throw new Exception("Bad!");
            }
        }

        private static void OnError(object sender, ErrorHandlerEventArgs args)
        {
            args.Action = ErrorAction.SkipMessage;
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
