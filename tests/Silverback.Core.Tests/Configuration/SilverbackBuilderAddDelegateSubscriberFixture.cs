// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

// TODO: FIX REDO
// IMPROVE TESTING EVERYTHING AND CHECKING COVERAGE
// MOVE FUNCTIONAL TESTS INTO PUBLISHER TESTS
//
// using System;
// using System.Collections.Generic;
// using System.Diagnostics.CodeAnalysis;
// using System.Linq;
// using System.Threading.Tasks;
// using FluentAssertions;
// using Microsoft.Extensions.DependencyInjection;
// using Silverback.Configuration;
// using Silverback.Messaging.Publishing;
// using Silverback.Messaging.Subscribers;
// using Silverback.Messaging.Subscribers.ArgumentResolvers;
// using Silverback.Messaging.Subscribers.Subscriptions;
// using Silverback.Tests.Core.TestTypes.Messages;
// using Silverback.Tests.Logging;
// using Xunit;
//
// namespace Silverback.Tests.Core.Configuration
// {
//     [SuppressMessage("ReSharper", "LocalFunctionCanBeMadeStatic", Justification = "Test code")]
//     public class SilverbackBuilderAddDelegateSubscriberFixture
//     {
//         private delegate void ProcessEventOneDelegate(TestEventOne message);
//
//         [Fact]
//         public void AddDelegateSubscriber_ShouldAddSubscriber_WhenDelegateIsPassed()
//         {
//             SilverbackBuilder builder = new ServiceCollection().AddSilverback();
//
//             void FakeSubscriber(TestEventOne message)
//             {
//             }
//
//             builder.AddDelegateSubscriber((ProcessEventOneDelegate)FakeSubscriber);
//
//             builder.BusOptions.Subscriptions.Should().HaveCount(1);
//             builder.BusOptions.Subscriptions[0].Should().BeOfType<DelegateSubscription>();
//
//             ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
//             IReadOnlyList<SubscribedMethod> methods = builder.BusOptions.Subscriptions[0].GetSubscribedMethods(serviceProvider);
//             methods.Should().HaveCount(1);
//             methods[0].MethodInfo.Name.Should().Be(nameof(FakeSubscriber));
//             methods[0].MessageType.Should().Be(typeof(TestEventOne));
//             methods[0].MessageArgumentResolver.Should().BeOfType<SingleMessageArgumentResolver>();
//             methods[0].
//         }
//
//         [Fact]
//         public void AddDelegateSubscriber_ShouldAddSubscriber_WhenActionIsPassed()
//         {
//             SilverbackBuilder builder = new ServiceCollection().AddSilverback();
//
//             void FakeSubscriber(TestEventOne message)
//             {
//             }
//
//             builder.AddDelegateSubscriber((Action<TestEventOne>)FakeSubscriber);
//
//             builder.BusOptions.Subscriptions.Should().HaveCount(1);
//             builder.BusOptions.Subscriptions[0].Should().BeOfType<DelegateSubscription>();
//
//             ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
//             IReadOnlyList<SubscribedMethod> methods = builder.BusOptions.Subscriptions[0].GetSubscribedMethods(serviceProvider);
//             methods.Should().HaveCount(1);
//             methods[0].MethodInfo.Name.Should().Be(nameof(FakeSubscriber));
//         }
//
//         [Fact]
//         public void AddDelegateSubscriber_ShouldAddSubscriber_WhenFuncReturningTaskIsPassed()
//         {
//             SilverbackBuilder builder = new ServiceCollection().AddSilverback();
//
//             Task FakeSubscriber(TestEventOne message) => Task.CompletedTask;
//
//             builder.AddDelegateSubscriber((Func<TestEventOne, Task>)FakeSubscriber);
//
//             builder.BusOptions.Subscriptions.Should().HaveCount(1);
//             builder.BusOptions.Subscriptions[0].Should().BeOfType<DelegateSubscription>();
//
//             ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
//             IReadOnlyList<SubscribedMethod> methods = builder.BusOptions.Subscriptions[0].GetSubscribedMethods(serviceProvider);
//             methods.Should().HaveCount(1);
//             methods[0].MethodInfo.Name.Should().Be(nameof(FakeSubscriber));
//         }
//
//         [Fact]
//         public void AddDelegateSubscriber_ShouldAddSubscriber_WhenFuncReturningObjectIsPassed()
//         {
//             SilverbackBuilder builder = new ServiceCollection().AddSilverback();
//
//             object? FakeSubscriber(TestEventOne message) => null;
//
//             builder.AddDelegateSubscriber((Func<TestEventOne, object?>)FakeSubscriber);
//
//             builder.BusOptions.Subscriptions.Should().HaveCount(1);
//             builder.BusOptions.Subscriptions[0].Should().BeOfType<DelegateSubscription>();
//
//             ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
//             IReadOnlyList<SubscribedMethod> methods = builder.BusOptions.Subscriptions[0].GetSubscribedMethods(serviceProvider);
//             methods.Should().HaveCount(1);
//             methods[0].MethodInfo.Name.Should().Be(nameof(FakeSubscriber));
//         }
//
//         [Fact]
//         public void AddDelegateSubscriber_ShouldAddSubscriber_WhenFuncReturningTaskWithObjectIsPassed()
//         {
//             SilverbackBuilder builder = new ServiceCollection().AddSilverback();
//
//             Task<object?> FakeSubscriber(TestEventOne message) => Task.FromResult<object?>(null);
//
//             builder.AddDelegateSubscriber((Func<TestEventOne, Task<object?>>)FakeSubscriber);
//
//             builder.BusOptions.Subscriptions.Should().HaveCount(1);
//             builder.BusOptions.Subscriptions[0].Should().BeOfType<DelegateSubscription>();
//
//             ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
//             IReadOnlyList<SubscribedMethod> methods = builder.BusOptions.Subscriptions[0].GetSubscribedMethods(serviceProvider);
//             methods.Should().HaveCount(1);
//             methods[0].MethodInfo.Name.Should().Be(nameof(FakeSubscriber));
//         }
//
//         // [Fact]
//         // public void AddDelegateSubscriber_ShouldAddSubscriber_WhenActionWithServiceProviderIsPassed()
//         // {
//         //     SilverbackBuilder builder = new ServiceCollection().AddSilverback();
//         //
//         //     builder.AddDelegateSubscriber((Action<TestEventOne>)ProcessEventOne);
//         //
//         //     builder.BusOptions.Subscriptions.Should().HaveCount(1);
//         //     builder.BusOptions.Subscriptions[0].Should().BeOfType<DelegateSubscription>();
//         //
//         //     ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
//         //     IReadOnlyList<SubscribedMethod> methods = builder.BusOptions.Subscriptions[0].GetSubscribedMethods(serviceProvider);
//         //     methods.Should().HaveCount(1);
//         //     methods[0].MethodInfo.Name.Should().Be(nameof(ProcessEventOne));
//         // }
//         //
//         // [Fact]
//         // public void AddDelegateSubscriber_ShouldAddSubscriber_WhenFuncIsPassed()
//         // {
//         //     SilverbackBuilder builder = new ServiceCollection().AddSilverback();
//         //
//         //     builder.AddDelegateSubscriber((Func<TestEventOne, Task>)ProcessEventOneAsync);
//         //
//         //     builder.BusOptions.Subscriptions.Should().HaveCount(1);
//         //     builder.BusOptions.Subscriptions[0].Should().BeOfType<DelegateSubscription>();
//         //
//         //     ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
//         //     IReadOnlyList<SubscribedMethod> methods = builder.BusOptions.Subscriptions[0].GetSubscribedMethods(serviceProvider);
//         //     methods.Should().HaveCount(1);
//         //     methods[0].MethodInfo.Name.Should().Be(nameof(ProcessEventOneAsync));
//         // }
//
//         [Fact]
//         public void
//             AddDelegateSubscriber_ActionWithServiceProvider_MessagesAndServiceProviderInstanceReceived()
//         {
//             int received = 0;
//
//             void Receive(TestEventOne message, IServiceProvider provider)
//             {
//                 received++;
//                 provider.Should().NotBeNull();
//             }
//
//             var serviceProvider = ServiceProviderHelper.GetServiceProvider(
//                 services => services
//                     .AddFakeLogger()
//                     .AddSilverback()
//                     .AddDelegateSubscriber((Action<TestEventOne, IServiceProvider>)Receive));
//
//             var publisher = serviceProvider.GetRequiredService<IPublisher>();
//             publisher.Publish(new TestEventOne());
//
//             received.Should().BeGreaterThan(0);
//         }
//
//         [Fact]
//         public void
//             AddDelegateSubscriber_FuncWithServiceProviderReturningTask_MessagesAndServiceProviderInstanceReceived()
//         {
//             int received = 0;
//
//             Task Receive(TestEventOne message, IServiceProvider provider)
//             {
//                 received++;
//                 provider.Should().NotBeNull();
//                 return Task.CompletedTask;
//             }
//
//             var serviceProvider = ServiceProviderHelper.GetServiceProvider(
//                 services => services
//                     .AddFakeLogger()
//                     .AddSilverback()
//                     .AddDelegateSubscriber((Func<TestEventOne, IServiceProvider, Task>)Receive));
//
//             var publisher = serviceProvider.GetRequiredService<IPublisher>();
//             publisher.Publish(new TestEventOne());
//
//             received.Should().BeGreaterThan(0);
//         }
//
//         [Fact]
//         public void
//             AddDelegateSubscriber_FuncWithServiceProviderReturningMessage_ServiceProviderInstanceReceived()
//         {
//             int received = 0;
//
//             TestEventTwo Receive(TestEventOne message, IServiceProvider provider)
//             {
//                 received++;
//                 provider.Should().NotBeNull();
//                 return new TestEventTwo();
//             }
//
//             var serviceProvider = ServiceProviderHelper.GetServiceProvider(
//                 services => services
//                     .AddFakeLogger()
//                     .AddSilverback()
//                     .AddDelegateSubscriber((Func<TestEventOne, IServiceProvider, TestEventTwo>)Receive));
//
//             var publisher = serviceProvider.GetRequiredService<IPublisher>();
//             publisher.Publish(new TestEventOne());
//
//             received.Should().BeGreaterThan(0);
//         }
//
//         [Fact]
//         public void
//             AddDelegateSubscriber_FuncWithServiceProviderReturningTaskWithMessage_ServiceProviderInstanceReceived()
//         {
//             int received = 0;
//
//             Task<TestEventTwo> Receive(TestEventOne message, IServiceProvider provider)
//             {
//                 received++;
//                 provider.Should().NotBeNull();
//                 return Task.FromResult(new TestEventTwo());
//             }
//
//             var serviceProvider = ServiceProviderHelper.GetServiceProvider(
//                 services => services
//                     .AddFakeLogger()
//                     .AddSilverback()
//                     .AddDelegateSubscriber((Func<TestEventOne, IServiceProvider, Task<TestEventTwo>>)Receive));
//
//             var publisher = serviceProvider.GetRequiredService<IPublisher>();
//             publisher.Publish(new TestEventOne());
//
//             received.Should().BeGreaterThan(0);
//         }
//
//     }
// }
