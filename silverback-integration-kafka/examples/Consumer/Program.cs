using System;
using System.Collections.Generic;
using Messages;
using Silverback;
using Silverback.Messaging.Broker;

namespace Consumer
{
    internal static class Program
    {
        private static void Main()
        {
            using (var broker = new KafkaBroker())
            {
                var configurations = new Dictionary<string, object>
                {
                    {"bootstrap.servers", "PLAINTEXT://192.168.99.100:29092"},
                    {"client.id", "ClientTest"},
                    { "group.id", "advanced-silverback-consumer" },
                    { "enable.auto.commit", true },
                    { "auto.commit.interval.ms", 5000 },
                    { "statistics.interval.ms", 60000 },
                    { "default.topic.config", new Dictionary<string, object>()
                        {
                            { "auto.offset.reset", "smallest" }
                        }
                    }
                };

                var endpoint = KafkaEndpoint.Create("Topic1", configurations);
                var consumer = broker.GetConsumer(endpoint);

                consumer.Received += (_, e) =>
                {
                    var message = (TestMessage)e.Message;
                    Console.WriteLine($"Received message {message.Id} from topic 'Topic1' with content '{message.Text}'.");
                };
                
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    broker.Disconnect();
                };

                broker.Connect();
            }

            PrintHeader();
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
