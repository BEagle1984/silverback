// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.ComponentModel.DataAnnotations;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes.Messages;

public class TestValidationMessage : IIntegrationEvent
{
    [StringLength(10)]
    public string String10 { get; set; } = null!;
}
