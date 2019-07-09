using System;
using System.Collections.Generic;
using System.Text;

namespace Silverback.Examples.Common.Messages
{
    public class MessageMovedEvent
    {
        public IEnumerable<Guid> Identifiers { get; set; }

        public string Source { get; set; }
        public string Destination { get; set; }
    }
}
