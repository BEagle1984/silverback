// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class StopConsumerErrorPolicyTests
    {
        private readonly ServiceProvider _serviceProvider;

        public StopConsumerErrorPolicyTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>());

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task HandleError_Whatever_FalseReturned()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task HandleError_Whatever_OffsetCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task HandleError_Whatever_TransactionAborted()
        {
            throw new NotImplementedException();
        }

    }
}
