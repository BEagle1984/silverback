// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging;

public class ProducerEndpointTests
{
    [Theory]
    [InlineData(null, "topic")]
    [InlineData("", "topic")]
    [InlineData("friendly", "friendly (topic)")]
    public void DisplayName_ShouldBeFriendlyNamePlusRawName(string? friendlyName, string expectedDisplayName)
    {
        TestProducerEndpoint configuration = new(
            "topic",
            new TestProducerEndpointConfiguration
            {
                FriendlyName = friendlyName
            });

        configuration.DisplayName.ShouldBe(expectedDisplayName);
    }
}
