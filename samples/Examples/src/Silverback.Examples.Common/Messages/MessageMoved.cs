using System;
using System.Collections.Generic;
using System.Text;

namespace Silverback.Examples.Common.Messages
{
    public class MessageMovedEvent
    {
        public Guid Id { get; set; }

        public string Destination { get; set; }
    }
}
