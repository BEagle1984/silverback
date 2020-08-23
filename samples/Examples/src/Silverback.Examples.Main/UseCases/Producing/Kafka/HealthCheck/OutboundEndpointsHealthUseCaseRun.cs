// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Silverback.Examples.Common;
using Silverback.Examples.Main.Menu;
using Silverback.Messaging.HealthChecks;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.HealthCheck
{
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Invoked by test framework")]
    public class OutboundEndpointsHealthUseCaseRun : IAsyncRunnable
    {
        private readonly IOutboundEndpointsHealthCheckService _service;

        public OutboundEndpointsHealthUseCaseRun(IOutboundEndpointsHealthCheckService service)
        {
            _service = service;
        }

        public async Task Run()
        {
            Console.ForegroundColor = Constants.PrimaryColor;
            Console.WriteLine("Pinging all endpoints...");
            ConsoleHelper.ResetColor();

            var result = await _service.PingAllEndpoints();

            Console.ForegroundColor = Constants.PrimaryColor;
            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            ConsoleHelper.ResetColor();
        }
    }
}
