// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging;

public class ConsumerEndpointFixture
{
    [Theory]
    [InlineData(null, "topic")]
    [InlineData("", "topic")]
    [InlineData("friendly", "friendly (topic)")]
    public void DisplayName_ShouldBeFriendlyNamePlusRawName(string? friendlyName, string expectedDisplayName)
    {
        TestConsumerEndpoint configuration = new(
            "topic",
            new TestConsumerEndpointConfiguration
            {
                FriendlyName = friendlyName
            });

        configuration.DisplayName.ShouldBe(expectedDisplayName);
    }
}
