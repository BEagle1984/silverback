// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class EndpointConfigurationFixture
{
    [Theory]
    [InlineData(null, "topic")]
    [InlineData("", "topic")]
    [InlineData("friendly", "friendly (topic)")]
    public void DisplayName_ShouldBeFriendlyNamePlusRawName(string? friendlyName, string expectedDisplayName)
    {
        TestProducerEndpointConfiguration configuration1 = new("topic")
        {
            FriendlyName = friendlyName
        };
        TestConsumerEndpointConfiguration configuration2 = new("topic")
        {
            FriendlyName = friendlyName
        };

        configuration1.DisplayName.ShouldBe(expectedDisplayName);
        configuration2.DisplayName.ShouldBe(expectedDisplayName);
    }
}
