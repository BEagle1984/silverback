using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Messages;

namespace Silverback.Integration.FileSystem.TestConsole
{
    public class TestMessage : IIntegrationMessage
    {
        public Guid Id { get; set; }

        public string Content { get; set; }
    }
}
