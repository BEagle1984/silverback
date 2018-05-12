using System;
using System.Collections.Generic;
using System.IO;
using Messages;
using Silverback;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Producer
{
    internal static class Program
    {
        private static void Main()
        {
            var configurations = new Dictionary<string, object>
            {
                {"bootstrap.servers", "PLAINTEXT://192.168.99.100:29092"},
                {"client.id", "ClientTest"},
                { "retries", 0},
                {"batch.num.messages", 1},
                {"socket.blocking.max.ms", 1},
                {"socket.nagle.disable", true},
                {"queue.buffering.max.ms", 0},
                {
                    "default.topic.config", new Dictionary<string, object>
                    {
                        {"acks", 0}
                    }
                }
            };

            using (var bus = new Bus())
            {
                bus.Subscribe(o => o.Subscribe(m => Console.WriteLine($"Message '{((TestMessage)m).Id}' delivered successfully!")));

                bus.Config()
                   .ConfigureBroker<KafkaBroker>(x => { })
                   .WithFactory(t => (IOutboundAdapter)Activator.CreateInstance(t))
                   .AddOutbound<TestMessage, KafkaOutboudAdapter>(KafkaEndpoint.Create("Topic1",configurations))
                   .ConnectBrokers();

                var cancelled = false;
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true; // prevent the process from terminating.
                    cancelled = true;
                    bus.DisconnectBrokers();
                };

                PrintUsage();

                while (!cancelled)
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
                        // Console returned null before 
                        // the CancelKeyPress was treated
                        break;
                    }
                    
                    bus.Publish(new TestMessage
                    {
                        Id = Guid.NewGuid(),
                        Text = text,
                        Type = "TestMessage"
                    });

                }            
            }
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
    }
}
