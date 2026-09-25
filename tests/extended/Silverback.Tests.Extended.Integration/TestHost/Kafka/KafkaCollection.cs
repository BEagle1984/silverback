// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Silverback.Tests.Extended.Integration.TestHost.Kafka;

[CollectionDefinition(Name, DisableParallelization = true)]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit collection fixture")]
public sealed class KafkaCollection : ICollectionFixture<KafkaFixture>
{
    public const string Name = "Kafka integration";
}
