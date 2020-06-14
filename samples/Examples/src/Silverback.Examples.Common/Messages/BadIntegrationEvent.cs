// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Common.Messages
{
    public class BadIntegrationEvent : IntegrationEvent
    {
        public int TryCount { get; set; } = 1;
    }
}