// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Diagnostics;

namespace Silverback.Tests.Integration.TestTypes;

public class SilverbackLoggerSubstitute<TCategory> : ISilverbackLogger<TCategory>
{
    public ILogger InnerLogger { get; } = Substitute.For<ILogger>();

    public bool IsEnabled(LogEvent logEvent) => false;
}
