// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker
{
    public class EndpointTests
    {
        [Fact]
        public void GetDisplayName_NotSet_NameReturned()
        {
            var endpoint = new TestConsumerEndpoint("name");

            endpoint.DisplayName.Should().Be("name");
        }

        [Fact]
        public void GetDisplayName_WithFriendlyName_FriendlyAndActualNameReturned()
        {
            var endpoint = new TestConsumerEndpoint("name");

            endpoint.FriendlyName = "display-name";

            endpoint.DisplayName.Should().Be("display-name (name)");
        }
    }
}
