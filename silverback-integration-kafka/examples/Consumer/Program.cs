using Messages;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using System;
using System.Collections.Generic;

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

            _broker.GetConsumer(new KafkaEndpoint("Topic1")
            {
                Configuration = new KafkaConfigurationDictionary
                {
                    {"bootstrap.servers", "PLAINTEXT://kafka:9092"},
                    {"client.id", "ClientTest"},
                    {"group.id", "advanced-silverback-consumer"},
                    {"enable.auto.commit", true},
                    {"auto.commit.interval.ms", 5000},
                    {"statistics.interval.ms", 60000},
                    {
                        "default.topic.config", new Dictionary<string, object>()
                        {
                            {"auto.offset.reset", "smallest"}
                        }
                    }
                }
            })
            .Received += OnMessageReceived;

            _broker.Connect();
        }
        private static void Disconnect()
        {
            _broker.Disconnect();
            _broker.Dispose();
        }

        private static void OnMessageReceived(object sender, IMessage message)
        {
            Console.WriteLine(
                message is TestMessage testMessage
                ? $"[{testMessage.Id}] {testMessage.Text}"
                : "Received a weird message!");
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
