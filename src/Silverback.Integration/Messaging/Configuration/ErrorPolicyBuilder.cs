// TODO: DELETE

// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System;
// using Microsoft.Extensions.DependencyInjection;
// using Silverback.Diagnostics;
// using Silverback.Messaging.Broker;
// using Silverback.Messaging.Inbound.ErrorHandling;
//
// namespace Silverback.Messaging.Configuration
// {
//     internal class ErrorPolicyBuilder : IErrorPolicyBuilder
//     {
//         // TODO: Replace with static class (must split policy configuration and actual logic) -> Policy.Retry.MaxFailedAttempts(4) and policy.GetImplementation(serviceProvider)
//
//         private readonly IServiceProvider _serviceProvider;
//
//         public ErrorPolicyBuilder(IServiceProvider serviceProvider)
//         {
//             _serviceProvider = serviceProvider;
//         }
//
//         public ErrorPolicyChain Chain(params ErrorPolicyBase[] policies) =>
//             new ErrorPolicyChain(
//                 _serviceProvider,
//                 _serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<ErrorPolicyChain>>(),
//                 policies);
//
//         public RetryErrorPolicy Retry(TimeSpan? initialDelay = null, TimeSpan? delayIncrement = null) =>
//             new RetryErrorPolicy(
//                 _serviceProvider,
//                 _serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<RetryErrorPolicy>>(),
//                 initialDelay,
//                 delayIncrement);
//
//         public SkipMessageErrorPolicy Skip() =>
//             new SkipMessageErrorPolicy(
//                 _serviceProvider,
//                 _serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<SkipMessageErrorPolicy>>());
//
//         public MoveMessageErrorPolicy Move(IProducerEndpoint endpoint) =>
//             new MoveMessageErrorPolicy(
//                 _serviceProvider.GetRequiredService<IBrokerCollection>(),
//                 endpoint,
//                 _serviceProvider,
//                 _serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<MoveMessageErrorPolicy>>());
//     }
// }
