// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Sequences.Batch;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Batch;

public class BatchSettingsTests
{
    [Theory]
    [InlineData(1, null)]
    [InlineData(10, 1d)]
    [InlineData(10, (double)int.MaxValue)]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid(int size, double? maxWaitTimeMilliseconds)
    {
        BatchSettings settings = new()
        {
            Size = size,
            MaxWaitTime = maxWaitTimeMilliseconds == null ? null : TimeSpan.FromMilliseconds(maxWaitTimeMilliseconds.Value)
        };

        Action act = settings.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void Validate_ShouldThrow_WhenSizeIsLessThanOne(int size)
    {
        BatchSettings settings = new()
        {
            Size = size
        };

        Action act = settings.Validate;

        act.ShouldThrow<BrokerConfigurationException>().Message.ShouldBe("Batch.Size must be greater than or equal to 1.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void Validate_ShouldThrow_WhenMaxWaitTimeIsNotGreaterThanZero(int maxWaitTimeMilliseconds)
    {
        BatchSettings settings = new()
        {
            Size = 10,
            MaxWaitTime = TimeSpan.FromMilliseconds(maxWaitTimeMilliseconds)
        };

        Action act = settings.Validate;

        act.ShouldThrow<BrokerConfigurationException>().Message.ShouldBe("Batch.MaxWaitTime must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxWaitTimeExceedsInt32MaxValueMilliseconds()
    {
        BatchSettings settings = new()
        {
            Size = 10,
            MaxWaitTime = TimeSpan.FromMilliseconds(int.MaxValue + 1d)
        };

        Action act = settings.Validate;

        act.ShouldThrow<BrokerConfigurationException>().Message
            .ShouldBe("Batch.MaxWaitTime must be less than or equal to Int32.MaxValue milliseconds.");
    }
}
