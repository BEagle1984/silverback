// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class StopConsumerErrorPolicyTests
    {
        // ReSharper disable once NotAccessedField.Local
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

        [Fact(Skip = "Not yet implemented")]
        public Task HandleError_Whatever_FalseReturned()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task HandleError_Whatever_OffsetCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task HandleError_Whatever_TransactionAborted()
        {
            throw new NotImplementedException();
        }
    }
}
