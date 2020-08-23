// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Examples.Common;
using Silverback.Examples.Main.Menu;
using Silverback.Messaging.HealthChecks;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.HealthCheck
{
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Invoked by test framework")]
    public class OutboundQueueHealthUseCaseRun : IAsyncRunnable
    {
        private readonly IOutboundQueueHealthCheckService _service;

        public OutboundQueueHealthUseCaseRun(IOutboundQueueHealthCheckService service)
        {
            _service = service;
        }

        public async Task Run()
        {
            Console.ForegroundColor = Constants.PrimaryColor;
            Console.WriteLine("Checking outbound queue (maxAge: 100ms, maxQueueLength: 1)...");
            ConsoleHelper.ResetColor();

            var result = await _service.CheckIsHealthy(TimeSpan.FromMilliseconds(100), 1);

            Console.ForegroundColor = Constants.PrimaryColor;
            Console.WriteLine($"Healthy: {result}");
            ConsoleHelper.ResetColor();
        }
    }
}
