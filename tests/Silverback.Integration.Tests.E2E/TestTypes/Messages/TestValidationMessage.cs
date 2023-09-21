using System.ComponentModel.DataAnnotations;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class TestValidationMessage : IIntegrationEvent
    {
        [StringLength(10)]
        public string String10 { get; set; } = null!;
    }
}
