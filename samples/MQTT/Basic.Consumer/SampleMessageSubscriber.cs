// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;
using Silverback.Samples.Mqtt.Basic.Common;

namespace Silverback.Samples.Mqtt.Basic.Consumer
{
    public class SampleMessageSubscriber
    {
        private readonly ILogger<SampleMessageSubscriber> _logger;

        public SampleMessageSubscriber(ILogger<SampleMessageSubscriber> logger)
        {
            _logger = logger;
        }

        public void OnMessageReceived(SampleMessage message) =>
            _logger.LogInformation($"Received {message.Number}");
    }
}
