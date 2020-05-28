// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Behaviors;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void ContainsAny_AlreadyAddedType_TrueIsReturned()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IBehavior, TestBehavior>();

            var result = services.ContainsAny(typeof(IBehavior));

            result.Should().BeTrue();
        }

        [Fact]
        public void ContainsAnyGeneric_AlreadyAddedType_TrueIsReturned()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IBehavior, TestBehavior>();

            var result = services.ContainsAny<IBehavior>();

            result.Should().BeTrue();
        }

        [Fact]
        public void ContainsAny_NotAddedType_FalseIsReturned()
        {
            var services = new ServiceCollection();
            services.AddSingleton<TestBehavior>();

            var result = services.ContainsAny(typeof(TestSortedBehavior));

            result.Should().BeFalse();
        }

        [Fact]
        public void ContainsAnyGeneric_NotAddedType_FalseIsReturned()
        {
            var services = new ServiceCollection();
            services.AddSingleton<TestBehavior>();

            var result = services.ContainsAny<TestSortedBehavior>();

            result.Should().BeFalse();
        }
    }
}
