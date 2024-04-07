// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Tests.Integration.E2E.TestHost;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class OutboundMessageEnrichmentFixture : MqttFixture
{
    public OutboundMessageEnrichmentFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }
}
