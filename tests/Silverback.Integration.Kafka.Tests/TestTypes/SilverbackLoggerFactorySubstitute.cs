// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Diagnostics;

namespace Silverback.Tests.Integration.Kafka.TestTypes;

public class SilverbackLoggerFactorySubstitute : ISilverbackLoggerFactory
{
    public ISilverbackLogger<TCategory> CreateLogger<TCategory>() => new SilverbackLoggerSubstitute<TCategory>();
}
