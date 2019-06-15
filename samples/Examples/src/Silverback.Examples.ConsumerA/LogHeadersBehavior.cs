// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.ConsumerA
{
    public class LogHeadersBehavior : IBehavior
    {
        private readonly ILogger<LogHeadersBehavior> _logger;

        public LogHeadersBehavior(ILogger<LogHeadersBehavior> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<object>> Handle(IEnumerable<object> messages, MessagesHandler next)
        {
            foreach (var message in messages.OfType<IInboundMessage>())
            {
                if (message.Headers != null && message.Headers.Any())
                {
                    _logger.LogInformation(
                        "Headers: {headers}",
                        string.Join(", ", message.Headers.Select(h => $"{h.Key}={h.Value}")));
                }
            }

            return await next(messages);
        }
    }
}
