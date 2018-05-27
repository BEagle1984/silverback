using System;
using System.Collections.Generic;
using Messages;
using Silverback;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Consumer
{
    internal static class Program
    {

        private static void Main()
        {
            PrintHeader();

            using (var bus = new Bus())
            {
                var configurations = new Dictionary<string, object>
                {
                    {"bootstrap.servers", "PLAINTEXT://192.168.99.100:29092"},
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
                };

                bus.Subscribe(new Subscriber());

                bus.Config()
                    .ConfigureBroker<KafkaBroker>(x => { })
                    .WithFactory(t => (IInboundAdapter) Activator.CreateInstance(t))
                    .AddInbound(new SimpleInboundAdapter(), KafkaEndpoint.Create("Topic1", configurations))
                    .ConnectBrokers();

                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    bus.DisconnectBrokers();
                };

                Console.ReadLine();
            }
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
