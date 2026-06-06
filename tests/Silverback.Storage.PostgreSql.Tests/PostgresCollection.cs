// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql;

[CollectionDefinition(nameof(PostgresCollection))]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Reviewed")]
public class PostgresCollection : ICollectionFixture<PostgresContainerFixture>;
