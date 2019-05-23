// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Silverback.Integration.Kafka.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Integration.Kafka.TestProducer
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
            var d = new DiagnosticListenerObserver(new DiagnosticObserver());
            DiagnosticListener.AllListeners.Subscribe(d);

            DiagnosticListener listener = new DiagnosticListener("SilverbackConsumerDiagnosticListener");


            var messageKeyProvider = new MessageKeyProvider(new[] { new DefaultPropertiesMessageKeyProvider() });
            _broker = new KafkaBroker(messageKeyProvider, GetLoggerFactory(), new MessageLogger(messageKeyProvider), listener);
            _broker.Connect();

            _producer = _broker.GetProducer(new KafkaProducerEndpoint("Topic1")
            {
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://kafka:9092"
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
            var headers = new List<MessageHeader>
            {
                new MessageHeader("CorrelationId", Guid.NewGuid().ToString()),
                new MessageHeader("CorrelationBaggage", "key1,value1;key2,value2")
            };

            producer.Produce(new TestMessage
            {
                Id = Guid.NewGuid(),
                Text = text,
                Type = "TestMessage"
            }, headers);
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


    public class DiagnosticObserver : IObserver<KeyValuePair<string, object>>
    {
        public void OnCompleted()
        {
            // Empty because this class is just a marker.
        }

        public void OnError(Exception error)
        {
            // Empty because this class is just a marker.
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            // Empty because this class is just a marker.
        }
    }

    public class DiagnosticListenerObserver : IObserver<DiagnosticListener>
    {
        private readonly DiagnosticObserver _diagnosticObserver;

        public DiagnosticListenerObserver(DiagnosticObserver diagnosticObserver)
        {
            _diagnosticObserver = diagnosticObserver;
        }

        public void OnCompleted()
        {
            // Intentionally do nothing.
        }

        public void OnError(Exception error)
        {
            // Intentionally do nothing.
        }

        public void OnNext(DiagnosticListener value)
        {
            switch (value.Name)
            {
                case "SilverbackConsumerDiagnosticListener":
                    value.Subscribe(_diagnosticObserver);
                    break;
            }
        }
    }
}
