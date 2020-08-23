// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Examples.Main.Menu;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Invoked by test framework")]
    public class BinaryFileUseCaseRun : IAsyncRunnable
    {
        private readonly IPublisher _publisher;

        public BinaryFileUseCaseRun(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public async Task Run()
        {
            await _publisher.PublishAsync(
                new BinaryFileMessage
                {
                    Content = new byte[] { 0xBE, 0xA6, 0x13, 0x19, 0x84 },
                    ContentType = "application/awesome"
                });
        }
    }
}
