// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Diagnostics;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Diagnostics;

public class StorageMemoryLoggerExtensionsTests
{
    [Fact]
    public void LoggerMessages_ShouldMatchLogEvents() =>
        LoggerMessagesAssert.ShouldMatchLogEvents(typeof(StorageMemoryLoggerExtensions), typeof(StorageMemoryLogEvents));
}
