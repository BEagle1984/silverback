using System;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Integration.FileSystem.TestConsole
{
    class Program
    {
        private const string BasePath = @"D:\Temp\Broker";

        static void Main(string[] args)
        {
            new Program().Execute();
        }

        private void Execute()
        {
            PrintLogo();

            Console.WriteLine("USAGE");
            Console.WriteLine();
            Console.WriteLine("produce <topic> <message>");
            Console.WriteLine("consume <topic>");
            Console.WriteLine();

            while (true)
            {
                Console.Write("? ");

                var command = Console.ReadLine().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);

                if (command.Length == 0)
                    continue;

                try
                {
                    switch (command[0].ToLower())
                    {
                        case "produce":
                            Produce(command[1], command[2]);
                            break;
                        case "consume":
                            Consume(command[1]);
                            break;
                        default:
                            WriteError("Unknown command!");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    WriteError(ex.Message);
                }
            }
        }

        private FileSystemBroker GetBroker() => new FileSystemBroker(NullLoggerFactory.Instance);

        private void Produce(string topicName, string messageContent)
        {
            var message = new TestMessage { Content = messageContent };
            var endpoint = FileSystemEndpoint.Create(topicName, BasePath);

            using (var broker = GetBroker())
            {
                broker.GetProducer(endpoint).Produce(Envelope.Create(message));
            }

            WriteSuccess($"Successfully produced message {message.Id} to topic '{topicName}'.");
        }

        private void Consume(string topicName)
        {
            var endpoint = FileSystemEndpoint.Create(topicName, BasePath);

            using (var broker = GetBroker())
            {
                var consumer = broker.GetConsumer(endpoint);

                broker.Connect();

                WriteSuccess($"Connected to topic '{topicName}'...");

                consumer.Received += (_, e) =>
                {
                    var message = (TestMessage)e.Message;
                    WriteSuccess($"Received message {message.Id} from topic '{topicName}' with content '{message.Content}'.");
                };

                while (Console.ReadKey(true).Key != ConsoleKey.Q)
                {
                    Thread.Sleep(100);
                }
            }
        }

        private void WriteError(string message)
            => WriteLine(message, ConsoleColor.Red);

        private void WriteSuccess(string message)
            => WriteLine(message, ConsoleColor.Green);

        private void WriteLine(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private void PrintLogo()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("   _|_|_|  _|  _|                                  _|                            _|        ");
            Console.WriteLine(" _|            _|  _|      _|    _|_|    _|  _|_|  _|_|_|      _|_|_|    _|_|_|  _|  _|    ");
            Console.WriteLine("   _|_|    _|  _|  _|      _|  _|_|_|_|  _|_|      _|    _|  _|    _|  _|        _|_|      ");
            Console.WriteLine("       _|  _|  _|    _|  _|    _|        _|        _|    _|  _|    _|  _|        _|  _|    ");
            Console.WriteLine(" _|_|_|    _|  _|      _|        _|_|_|  _|        _|_|_|      _|_|_|    _|_|_|  _|    _|  ");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.ResetColor();
        }
    }
}
