// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Silverback.Examples.Common.Messages;
using Silverback.Examples.Main.Menu;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Basic
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Invoked by test framework")]
    public class TranslateUseCaseRun : IAsyncRunnable
    {
        private readonly IEventPublisher _eventPublisher;

        public TranslateUseCaseRun(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        public async Task Run()
        {
            await _eventPublisher.PublishAsync(
                new SimpleEvent
                {
                    Content = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                });
        }
    }
}
