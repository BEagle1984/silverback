// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Common.Messages
{
    public class MessageMovedEvent
    {
        public string? Source { get; set; }

        public string? Destination { get; set; }
    }
}
