using System;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Domain
{
    public class TestEventThree : IIntegrationEvent
    {
        public string Content { get; set; }
        public Guid Id { get; set; }
    }
}