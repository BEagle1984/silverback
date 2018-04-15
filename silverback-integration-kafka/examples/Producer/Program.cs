using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Producer
{
    internal static class Program
    {
        private static void Main()
        {
            BrokersConfig.Instance.Add<KafkaBroker>(c =>
                c.SetConnection(new Dictionary<string, object>
                    {
                        {"bootstrap.servers", "PLAINTEXT://192.168.99.100:29092"},
                        {"client.id", "ClientTest"}
                    })
                    .WithPublicationStrategy(new Dictionary<string, object>
                    {
                        {"retries", 0},
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
                    })
                    .SetDeliveryHandlerFor("Topic1", () => new DeliveryReportHandler("./DeliveryReport.log"))
                    .ValidateConfiguration());

            var endpoints = new List<BasicEndpoint>
            {
                BasicEndpoint.Create("Topic1"),
                BasicEndpoint.Create("Topic2")
            };

            PrintUsage();

            var cancelled = false;
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true; // prevent the process from terminating.
                cancelled = true;
            };

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

                var topic = "Topic1";
                var val = text;

                // split line if both topic and value specified.
                var index = text.IndexOf(" ", StringComparison.Ordinal);
                if (index != -1)
                {
                    topic = text.Substring(0, index);
                    val = text.Substring(index + 1);
                }
                
                endpoints
                    .First(e => e.Name == topic)
                    .GetProducer()
                    .Produce(Envelope.Create(new TestMessage{

                        Id = Guid.NewGuid(),
                        Text = val,
                        Type = "TestMessage"
                    }));
            }
        }

        private static void PrintUsage()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(@"  _  __      __ _           _____               _                     ");
            Console.WriteLine(@" | |/ /     / _| |         |  __ \             | |                    ");
            Console.WriteLine(@" | ' / __ _| |_| | ____ _  | |__) | __ ___   __| |_   _  ___ ___ _ __ ");
            Console.WriteLine(@" |  < / _` |  _| |/ / _` | |  ___/ '__/ _ \ / _` | | | |/ __/ _ \ '__|");
            Console.WriteLine(@" | . \ (_| | | |   < (_| | | |   | | | (_) | (_| | |_| | (_|  __/ |   ");
            Console.WriteLine(@" |_|\_\__,_|_| |_|\_\__,_| |_|   |_|  \___/ \__,_|\__,_|\___\___|_|   ");
            Console.ResetColor();
            Console.WriteLine("\nTo post a message to the default topic:");
            Console.WriteLine("> message<Enter>");
            Console.WriteLine("To post a message to another topic:");
            Console.WriteLine("> topic message<Enter>");
            Console.WriteLine("Ctrl-C to quit.\n");
        }
    }
}
