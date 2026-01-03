// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Tests.Integration.E2E.TestHost;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class SchemaRegistryTests : KafkaTests
{
    public SchemaRegistryTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }
}
