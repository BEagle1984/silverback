// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Tests.Integration.E2E.TestHost;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class OutboxFixture : KafkaFixture
{
    public OutboxFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }
}
